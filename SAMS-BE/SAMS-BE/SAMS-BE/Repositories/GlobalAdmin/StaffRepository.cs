using System.Data.Common;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Infrastructure.Persistence.Global;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Mappers.Staff;
using SAMS_BE.Models;
using SAMS_BE.Tenant;
using SAMS_BE.Utils;

namespace SAMS_BE.Repositories.GlobalAdmin
{
    public sealed class StaffRepository(
    GlobalDirectoryContext globalDb,
    BuildingManagementContext db,
     ITenantContextAccessor schemaSwitcher,
    IMapper mapper) : IStaffRepository
    {
        public async Task<(List<StaffListItemDto> Items, int Total)> SearchAsync(
       string schema, StaffQuery staffQuery, CancellationToken ct)
        {
            try
            {
                schemaSwitcher.SetSchema(schema);

                var q = db.Set<StaffProfile>().AsNoTracking();

                if (!string.IsNullOrWhiteSpace(staffQuery.Search))
                {
                    var keyword = staffQuery.Search.Trim();
                    q = q.Where(s =>
                        (s.User != null && (
                            EF.Functions.Like(((s.User.FirstName ?? "") + " " + (s.User.LastName ?? "")), $"%{keyword}%")
                            || EF.Functions.Like(s.User.Email ?? "", $"%{keyword}%")
                            || EF.Functions.Like(s.User.Phone ?? "", $"%{keyword}%")
                        ))
                        || EF.Functions.Like(s.Role.RoleName ?? "", $"%{keyword}%")
                        || EF.Functions.Like(s.Role.RoleKey ?? "", $"%{keyword}%")
                    );
                }

                if (staffQuery.RoleId.HasValue)
                {
                    q = q.Where(s => s.RoleId == staffQuery.RoleId.Value);
                }

                q = q.OrderByDescending(s => s.TerminationDate == null)
                     .ThenBy(s => s.User!.FirstName)
                     .ThenBy(s => s.User!.LastName);

                var total = await q.CountAsync(ct);
                if (total == 0) return (new List<StaffListItemDto>(), 0);

                var pageNumber = Math.Max(1, staffQuery.Page);
                var pageSize = Math.Clamp(staffQuery.PageSize, 1, 200);

                var items = await q
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(s => new StaffListItemDto
                    {
                        StaffCode = s.StaffCode,
                        UserId = s.UserId,
                        FirstName = s.User != null ? (s.User.FirstName ?? "") : "",
                        LastName = s.User != null ? (s.User.LastName ?? "") : "",
                        FullName = s.User != null ? ($"{(s.User.FirstName ?? "")} {(s.User.LastName ?? "")}").Trim() : "",
                        Email = s.User != null ? (s.User.Email ?? "") : "",
                        Phone = s.User != null ? (s.User.Phone ?? "") : "",

                        Role = s.Role != null ? (s.Role.RoleName ?? s.Role.RoleKey) : null,

                        TerminationDate = s.TerminationDate.HasValue
                            ? s.TerminationDate.Value.ToDateTime(TimeOnly.MinValue)
                            : (DateTime?)null
                    })
                    .ToListAsync(ct);

                return (items, total);
            }
            catch (DbException ex) when (DbExceptionUtils.IsMissingSchemaOrTable(ex))
            {
                return (new List<StaffListItemDto>(), 0);
            }
            catch (Exception ex) when (DbExceptionUtils.IsMissingSchemaOrTableDeep(ex))
            {
                return (new List<StaffListItemDto>(), 0);
            }
        }
        public async Task<StaffDetailDto?> GetDetailAsync(string schema, Guid staffCode, CancellationToken ct)
        {
            schemaSwitcher.SetSchema(schema);

            var staffEntity = await db.Set<StaffProfile>()
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Role)
                .Where(s => s.StaffCode == staffCode)
                .FirstOrDefaultAsync(ct);

            return staffEntity?.ToStaffDetailDto();

        }

        public async Task<bool> UpdateAsync(string schema, Guid staffCode, StaffUpdateDto dto, string avatarUrl, string cardUrl, CancellationToken ct)
        {
            schemaSwitcher.SetSchema(schema);

            var staff = await db.StaffProfiles.Include(s => s.User).FirstOrDefaultAsync(s => s.StaffCode == staffCode, ct);

            if (staff == null || staff.User == null)
            {
                return false;
            }

            // map
            dto.MapToUser(staff.User, avatarUrl);
            dto.MapToStaffProfile(staff, cardUrl);

            await db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ActivateAsync(string schema, Guid staffCode, CancellationToken ct)
        {
            schemaSwitcher.SetSchema(schema);

            var staff = await db.Set<StaffProfile>().Include(u => u.User).FirstOrDefaultAsync(x => x.StaffCode == staffCode, ct);
            if (staff is null) return false;

            var accountGlobal = await globalDb.Set<user_building>().Include(u => u.keycloak_user).FirstOrDefaultAsync(x => x.keycloak_user_id.Equals(staff.UserId));
            if (accountGlobal is null) return false;

            staff.TerminationDate = null;
            staff.IsActive = true;

            staff.User.UpdatedAt = DateTime.UtcNow;

            accountGlobal.update_at = DateTime.UtcNow;

            accountGlobal.status = 1;

            accountGlobal.keycloak_user.update_at = accountGlobal.update_at;

            await db.SaveChangesAsync(ct);

            await globalDb.SaveChangesAsync(ct);

            return true;
        }

        public async Task<bool> DeactivateAsync(string schema, Guid staffCode, DateTime? date, CancellationToken ct)
        {
            schemaSwitcher.SetSchema(schema);

            var staff = await db.Set<StaffProfile>().Include(u => u.User).FirstOrDefaultAsync(x => x.StaffCode == staffCode, ct);
            if (staff is null) return false;

            var accountGlobal = await globalDb.Set<user_building>().Include(u => u.keycloak_user).FirstOrDefaultAsync(x => x.keycloak_user_id.Equals(staff.UserId));
            if (accountGlobal is null) return false;

            staff.TerminationDate = date.HasValue
                                    ? DateOnly.FromDateTime(date.Value)
                                    : DateOnly.FromDateTime(DateTime.UtcNow);
            staff.IsActive = false;

            staff.User.UpdatedAt = DateTime.UtcNow;

            accountGlobal.update_at = DateTime.UtcNow;

            accountGlobal.status = 0;

            accountGlobal.keycloak_user.update_at = accountGlobal.update_at;

            await db.SaveChangesAsync(ct);
            await globalDb.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ExistsTaxCodeAsync(string schema, string? taxCode, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(taxCode))
            {
                return false;
            }

            schemaSwitcher.SetSchema(schema);
            taxCode = taxCode.Trim();

            return await db.Set<StaffProfile>()
                .AsNoTracking()
                .AnyAsync(s => s.TaxCode == taxCode, ct);
        }

        public async Task<bool> ExistsSocialInsuranceNoAsync(string schema, string? socialInsuranceNo, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(socialInsuranceNo))
            {
                return false;
            }

            schemaSwitcher.SetSchema(schema);
            socialInsuranceNo = socialInsuranceNo.Trim();

            return await db.Set<StaffProfile>()
                .AsNoTracking()
                .AnyAsync(s => s.SocialInsuranceNo == socialInsuranceNo, ct);
        }
    }
}

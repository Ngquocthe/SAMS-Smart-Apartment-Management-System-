using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Interfaces.IRepository.GlobalAdmin;
using SAMS_BE.Models;
using SAMS_BE.Tenant;

namespace SAMS_BE.Repositories.GlobalAdmin
{
    public sealed class WorkRoleRepository(
    BuildingManagementContext dbContext,
    ITenantContextAccessor tenantAccessor
) : IWorkRoleRepository
    {
        public async Task<List<WorkRoleOptionDto>> GetRolesAsync(
            string schema,
            string? search,
            bool includeInactive,
            CancellationToken ct)
        {
            tenantAccessor.SetSchema(schema);

            var q = dbContext.Set<WorkRole>().AsNoTracking();

            if (!includeInactive)
                q = q.Where(r => r.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();
                q = q.Where(r =>
                    EF.Functions.Like(r.RoleName, $"%{keyword}%") ||
                    EF.Functions.Like(r.RoleKey, $"%{keyword}%"));
            }

            return await q
                .OrderBy(r => r.RoleName)
                .Select(r => new WorkRoleOptionDto
                {
                    RoleId = r.RoleId,
                    RoleKey = r.RoleKey,
                    RoleName = r.RoleName,
                    IsActive = r.IsActive
                })
                .ToListAsync(ct);
        }
    }
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IRepository.Resident;
using SAMS_BE.Models;
using SAMS_BE.Tenant;
using SendGrid.Helpers.Mail;

namespace SAMS_BE.Repositories.Resident
{
    public class ResidentRepository : IResidentRepository
    {
        private readonly BuildingManagementContext _db;
        private readonly ITenantContextAccessor _tenantAccessor;

        public ResidentRepository(BuildingManagementContext db, ITenantContextAccessor tenantAccessor)
        {
            _db = db;
            _tenantAccessor = tenantAccessor;
        }

        public async Task CreateResidentAsync(string schema, ResidentProfile residentProfile, CancellationToken ct)
        {
            _tenantAccessor.SetSchema(schema);

            await _db.ResidentProfiles.AddAsync(residentProfile, ct);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsEmailAsync(string schema, string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            _tenantAccessor.SetSchema(schema);

            return await _db.ResidentProfiles
                .AsNoTracking()
                .AnyAsync(r => r.Email == email, ct);
        }

        public async Task<bool> ExistsPhoneAsync(string schema, string phone, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            _tenantAccessor.SetSchema(schema);

            return await _db.ResidentProfiles
                .AsNoTracking()
                .AnyAsync(r => r.Phone == phone, ct);
        }

        public async Task<bool> ExistsIdNumberAsync(string schema, string idNumber, CancellationToken ct)
        {
            var sql = $"""
                SELECT 1
                FROM [{schema}].[resident_profiles]
                WHERE id_number = @id
            """;

            var conn = _db.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = idNumber;
            cmd.Parameters.Add(p);

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            var result = await cmd.ExecuteScalarAsync(ct);
            return result != null;
        }

        public async Task<bool> ApartmentExistsAsync(string schema, Guid apartmentId, CancellationToken ct)
        {
            _tenantAccessor.SetSchema(schema);

            return await _db.Apartments
                .AsNoTracking()
                .AnyAsync(a => a.ApartmentId == apartmentId, ct);
        }

        public async Task<(bool hasOwner, bool hasPrimary)> GetApartmentOwnershipStatusAsync(
            string schema,
            Guid apartmentId,
            CancellationToken ct)
        {
            _tenantAccessor.SetSchema(schema);

            var relations = await _db.ResidentApartments
                .AsNoTracking()
                .Where(ra => ra.ApartmentId == apartmentId)
                .ToListAsync(ct);

            var hasOwner = relations.Any(ra => ra.RelationType == Utils.Constant.Resident.Owner);
            var hasPrimary = relations.Any(ra => ra.IsPrimary);

            return (hasOwner, hasPrimary);
        }

        public async Task<ResidentProfile?> GetResidentByIdNumberAsync(string schema, string idNumber, CancellationToken ct)
        {
            var sql = $"""
                SELECT 
                    resident_id,
                    user_id,
                    full_name,
                    phone,
                    email,
                    id_number,
                    dob,
                    gender,
                    address,
                    status,
                    is_verified,
                    verified_at,
                    nationality,
                    internal_note,
                    meta,
                    created_at,
                    updated_at
                FROM [{schema}].[resident_profiles]
                WHERE id_number = @id
            """;

            var conn = _db.Database.GetDbConnection();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            var p = cmd.CreateParameter();
            p.ParameterName = "@id";
            p.Value = idNumber;
            cmd.Parameters.Add(p);

            if (conn.State != ConnectionState.Open)
                await conn.OpenAsync(ct);

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            
            if (!await reader.ReadAsync(ct))
            {
                return null;
            }

            return new ResidentProfile
            {
                ResidentId = reader.GetGuid(reader.GetOrdinal("resident_id")),
                UserId = reader.IsDBNull(reader.GetOrdinal("user_id")) ? null : reader.GetGuid(reader.GetOrdinal("user_id")),
                FullName = reader.GetString(reader.GetOrdinal("full_name")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                IdNumber = reader.IsDBNull(reader.GetOrdinal("id_number")) ? null : reader.GetString(reader.GetOrdinal("id_number")),
                Dob = reader.IsDBNull(reader.GetOrdinal("dob")) ? null : DateOnly.FromDateTime(reader.GetDateTime(reader.GetOrdinal("dob"))),
                Gender = reader.IsDBNull(reader.GetOrdinal("gender")) ? null : reader.GetString(reader.GetOrdinal("gender")),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString(reader.GetOrdinal("address")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                IsVerified = reader.IsDBNull(reader.GetOrdinal("is_verified")) ? false : reader.GetBoolean(reader.GetOrdinal("is_verified")),
                VerifiedAt = reader.IsDBNull(reader.GetOrdinal("verified_at")) ? null : reader.GetDateTime(reader.GetOrdinal("verified_at")),
                Nationality = reader.IsDBNull(reader.GetOrdinal("nationality")) ? null : reader.GetString(reader.GetOrdinal("nationality")),
                InternalNote = reader.IsDBNull(reader.GetOrdinal("internal_note")) ? null : reader.GetString(reader.GetOrdinal("internal_note")),
                Meta = reader.IsDBNull(reader.GetOrdinal("meta")) ? null : reader.GetString(reader.GetOrdinal("meta")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }


        public async Task<ResidentProfile?> GetByIdAsync(Guid residentId, CancellationToken ct = default)
        {
            return await _db.ResidentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ResidentId == residentId, ct);
        }

        public async Task<ResidentProfile?> GetWithApartmentsAsync(Guid residentId, CancellationToken ct = default)
        {
            return await _db.ResidentProfiles
                .AsNoTracking()
                .Include(r => r.ResidentApartments)
                    .ThenInclude(ra => ra.Apartment)
                .FirstOrDefaultAsync(r => r.ResidentId == residentId, ct);
        }

        public async Task<IEnumerable<ResidentProfile>> GetByApartmentIdAsync(Guid apartmentId, CancellationToken ct = default)
        {
            var query = _db.ResidentApartments
                .AsNoTracking()
                .Where(ra => ra.ApartmentId == apartmentId)
                .Select(ra => ra.Resident)
                .Distinct();

            return await query
                .Include(r => r.ResidentApartments)
                    .ThenInclude(ra => ra.Apartment)
                .ToListAsync(ct);
        }

        public async Task<PagedResult<ResidentProfile>> GetResidentsPagedAsync(
            string schema,
            int pageNumber,
            int pageSize,
            Guid? apartmentId = null,
            string? q = null,
            string? sortBy = null,
            string? sortDir = null,
            CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(schema))
            {
                _tenantAccessor.SetSchema(schema);
            }

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 20;

            // base query (distinct residents if filtered by apartment)
            IQueryable<ResidentProfile> baseQuery;
            if (apartmentId.HasValue)
            {
                baseQuery = _db.ResidentApartments
                    .AsNoTracking()
                    .Where(ra => ra.ApartmentId == apartmentId.Value)
                    .Select(ra => ra.Resident)
                    .Distinct();
            }
            else
            {
                baseQuery = _db.ResidentProfiles.AsNoTracking();
            }

            // apply search query if provided (search in FullName, Phone, Email, IdNumber)
            if (!string.IsNullOrWhiteSpace(q))
            {
                var qTrim = q.Trim();
                baseQuery = baseQuery.Where(r =>
                    (r.FullName != null && r.FullName.Contains(qTrim)) ||
                    (r.Phone != null && r.Phone.Contains(qTrim)) ||
                    (r.Email != null && r.Email.Contains(qTrim)) ||
                    (r.IdNumber != null && r.IdNumber.Contains(qTrim))
                );
            }

            // compute total count BEFORE paging
            var totalCount = await baseQuery.CountAsync(ct);

            // include related apartments for returned items
            var query = baseQuery
                .Include(r => r.ResidentApartments)
                    .ThenInclude(ra => ra.Apartment);

            // sorting (default by FullName asc)
            var sort = (sortBy ?? "fullname").ToLowerInvariant();
            var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);

            IOrderedQueryable<ResidentProfile> ordered;
            switch (sort)
            {
                case "createdat":
                case "created_at":
                    ordered = desc ? query.OrderByDescending(r => r.CreatedAt) : query.OrderBy(r => r.CreatedAt);
                    break;
                case "updatedat":
                case "updated_at":
                    ordered = desc ? query.OrderByDescending(r => r.UpdatedAt) : query.OrderBy(r => r.UpdatedAt);
                    break;
                case "fullname":
                default:
                    ordered = desc ? query.OrderByDescending(r => r.FullName) : query.OrderBy(r => r.FullName);
                    break;
            }

            var items = await ordered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var result = new PagedResult<ResidentProfile>
            {
                Items = items,
                TotalCount = totalCount,
                TotalItems = items.Count,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };

            return result;
        }
    }
}

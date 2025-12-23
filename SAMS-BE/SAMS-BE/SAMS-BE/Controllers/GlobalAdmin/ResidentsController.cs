using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Models;
using SAMS_BE.Tenant;

namespace SAMS_BE.Controllers.GlobalAdmin
{
    [ApiController]
    [Route("api/{schema}/Residents")]
    public class ResidentsController : ControllerBase
    {
        private readonly BuildingManagementContext _db;
        private readonly ITenantContextAccessor _tenantAccessor;

        public ResidentsController(BuildingManagementContext db, ITenantContextAccessor tenantAccessor)
        {
            _db = db;
            _tenantAccessor = tenantAccessor;
        }

        /// <summary>
        /// Lấy danh sách cư dân có phân trang
        /// GET /api/{schema}/Residents/paged
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PagedApiResponse<ResidentDto>>> GetPaged(
            [FromRoute] string schema,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? apartmentId = null,
            [FromQuery] string? q = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = null)
        {
            try
            {
                // Set schema for tenant context
                _tenantAccessor.SetSchema(schema);

                // =========================
                // 1. Base entity query
                // =========================
                var query = _db.Set<ResidentProfile>()
                    .AsNoTracking()
                    .Include(rp => rp.User)
                    .AsQueryable();

                // =========================
                // 2. Filter by Apartment
                // =========================
                if (apartmentId.HasValue)
                {
                    var residentIdsWithApartment = _db.Set<ResidentApartment>()
                        .AsNoTracking()
                        .Where(ra => ra.ApartmentId == apartmentId.Value)
                        .Select(ra => ra.ResidentId);

                    query = query.Where(rp => residentIdsWithApartment.Contains(rp.ResidentId));
                }

                // =========================
                // 3. Search
                // =========================
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim().ToLower();
                    query = query.Where(rp =>
                        (rp.FullName != null && rp.FullName.ToLower().Contains(term)) ||
                        (rp.Phone != null && rp.Phone.Contains(term)) ||
                        (rp.Email != null && rp.Email.ToLower().Contains(term)) ||
                        (rp.IdNumber != null && rp.IdNumber.Contains(term))
                    );
                }

                // =========================
                // 4. Total count
                // =========================
                var totalCount = await query.CountAsync();

                // =========================
                // 5. Sorting
                // =========================
                var isDesc = sortDir?.ToLower() == "desc";

                query = sortBy?.ToLower() switch
                {
                    "fullname" => isDesc
                        ? query.OrderByDescending(rp => rp.FullName)
                        : query.OrderBy(rp => rp.FullName),

                    "createdat" => isDesc
                        ? query.OrderByDescending(rp => rp.CreatedAt)
                        : query.OrderBy(rp => rp.CreatedAt),

                    "phone" => isDesc
                        ? query.OrderByDescending(rp => rp.Phone)
                        : query.OrderBy(rp => rp.Phone),

                    "email" => isDesc
                        ? query.OrderByDescending(rp => rp.Email)
                        : query.OrderBy(rp => rp.Email),

                    _ => query.OrderBy(rp => rp.FullName)
                };

                // =========================
                // 6. Paging + Projection
                // =========================
                var items = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(rp => new ResidentDto
                    {
                        ResidentId = rp.ResidentId,
                        UserId = rp.UserId,
                        FullName = rp.FullName,
                        Phone = rp.Phone,
                        Email = rp.Email,
                        IdNumber = rp.IdNumber,
                        Dob = rp.Dob,
                        Gender = rp.Gender,
                        Address = rp.Address,
                        Status = rp.Status,
                        CreatedAt = rp.CreatedAt,
                        UpdatedAt = rp.UpdatedAt,

                        HasFaceRegistered =
                            rp.User != null &&
                            rp.User.FaceEmbedding != null &&
                            rp.User.FaceEmbedding.Length > 0,

                        User = rp.User == null ? null : new ResidentUserDto
                        {
                            UserId = rp.User.UserId,
                            Username = rp.User.Username ?? "",
                            Email = rp.User.Email ?? "",
                            Phone = rp.User.Phone ?? "",
                            FirstName = rp.User.FirstName ?? "",
                            LastName = rp.User.LastName ?? "",
                            Dob = rp.User.Dob,
                            Address = rp.User.Address,
                            AvatarUrl = rp.User.AvatarUrl,
                            CheckinPhotoUrl = rp.User.CheckinPhotoUrl
                        },

                        Apartments = new List<ResidentApartmentDto>() // Will be populated below
                    })
                    .ToListAsync();

                // Load Apartments separately for each resident
                if (items.Any())
                {
                    var residentIds = items.Select(r => r.ResidentId).ToList();
                    var apartments = await _db.Set<ResidentApartment>()
                        .AsNoTracking()
                        .Where(ra => residentIds.Contains(ra.ResidentId))
                        .Include(ra => ra.Apartment)
                        .Select(ra => new
                        {
                            ra.ResidentId,
                            Apartment = new ResidentApartmentDto
                            {
                                ResidentApartmentId = ra.ResidentApartmentId,
                                ApartmentId = ra.ApartmentId,
                                RelationType = ra.RelationType,
                                StartDate = ra.StartDate,
                                EndDate = ra.EndDate,
                                IsPrimary = ra.IsPrimary,
                                ApartmentNumber = ra.Apartment != null ? ra.Apartment.Number : null
                            }
                        })
                        .ToListAsync();

                    // Group apartments by ResidentId and assign to each resident
                    var apartmentsByResident = apartments
                        .GroupBy(a => a.ResidentId)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.Apartment).ToList());

                    foreach (var item in items)
                    {
                        if (apartmentsByResident.TryGetValue(item.ResidentId, out var residentApartments))
                        {
                            item.Apartments = residentApartments;
                        }
                    }
                }

                // =========================
                // 7. Response
                // =========================
                return Ok(new PagedApiResponse<ResidentDto>(
                    items,
                    totalCount,
                    pageNumber,
                    pageSize
                ));
            }
            catch (Exception ex)
            {
                // Log ở đây nếu có ILogger
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }
    }
}

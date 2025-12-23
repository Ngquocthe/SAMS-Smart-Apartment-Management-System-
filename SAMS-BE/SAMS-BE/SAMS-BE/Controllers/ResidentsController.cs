using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.DTOs.Request.Resident;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Interfaces.IService.IResident;
using SAMS_BE.Models;
using SAMS_BE.Services.Resident;

namespace SAMS_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ResidentsController : ControllerBase
    {
        private readonly BuildingManagementContext _db;
        private readonly IResidentService residentService;

        public ResidentsController(BuildingManagementContext db, IResidentService residentService)
        {
            _db = db;
            this.residentService = residentService;
        }

        [HttpGet("{residentId:guid}")]
        public async Task<IActionResult> GetByResidentId(Guid residentId)
        {
            var dto = await BuildResidentDtoQuery()
                .FirstOrDefaultAsync(x => x.ResidentId == residentId);

            return dto == null ? NotFound() : Ok(dto);
        }

        [HttpGet("by-user/{userId:guid}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var dto = await BuildResidentDtoQuery()
                .FirstOrDefaultAsync(x => x.UserId == userId);

            return dto == null ? NotFound() : Ok(dto);
        }

        /// <summary>
        /// Lấy tất cả cư dân với thông tin căn hộ
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await BuildResidentDtoQuery()
                .OrderBy(x => x.FullName)
                .ToListAsync();

            return Ok(result);
        }

        /// <summary>
        /// Lấy danh sách cư dân thuộc về một căn hộ
        /// </summary>
        [HttpGet("apartment/{apartmentId:guid}")]
        public async Task<IActionResult> GetByApartmentId(Guid apartmentId)
        {
            var query = from rp in _db.ResidentProfiles.AsNoTracking()
                        join u in _db.Users.AsNoTracking() on rp.UserId equals u.UserId into ujoin
                        from u in ujoin.DefaultIfEmpty()
                        where _db.ResidentApartments.Any(ra => ra.ApartmentId == apartmentId && ra.ResidentId == rp.ResidentId)
                        select new ResidentDto
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
                            HasFaceRegistered = u != null && u.FaceEmbedding != null && u.FaceEmbedding.Length > 0,
                            User = u == null ? null : new ResidentUserDto
                            {
                                UserId = u.UserId,
                                Username = u.Username,
                                Email = u.Email,
                                Phone = u.Phone,
                                FirstName = u.FirstName,
                                LastName = u.LastName,
                                Dob = u.Dob,
                                Address = u.Address,
                                AvatarUrl = u.AvatarUrl,
                                CheckinPhotoUrl = u.CheckinPhotoUrl
                            },
                            Apartments = (from ra in _db.ResidentApartments.AsNoTracking()
                                          where ra.ResidentId == rp.ResidentId && ra.ApartmentId == apartmentId
                                          join a in _db.Apartments.AsNoTracking() on ra.ApartmentId equals a.ApartmentId
                                          select new ResidentApartmentDto
                                          {
                                              ResidentApartmentId = ra.ResidentApartmentId,
                                              ApartmentId = ra.ApartmentId,
                                              RelationType = ra.RelationType,
                                              StartDate = ra.StartDate,
                                              EndDate = ra.EndDate,
                                              IsPrimary = ra.IsPrimary,
                                              ApartmentNumber = a.Number
                                          }).ToList()
                        };

            var result = await query.ToListAsync();
            return Ok(result);
        }

        private IQueryable<ResidentDto> BuildResidentDtoQuery()
        {
            var q = from rp in _db.ResidentProfiles.AsNoTracking()
                    join u in _db.Users.AsNoTracking() on rp.UserId equals u.UserId into ujoin
                    from u in ujoin.DefaultIfEmpty()
                    select new ResidentDto
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
                        HasFaceRegistered = u != null && u.FaceEmbedding != null && u.FaceEmbedding.Length > 0,
                        User = u == null ? null : new ResidentUserDto
                        {
                            UserId = u.UserId,
                            Username = u.Username,
                            Email = u.Email,
                            Phone = u.Phone,
                            FirstName = u.FirstName,
                            LastName = u.LastName,
                            Dob = u.Dob,
                            Address = u.Address,
                            AvatarUrl = u.AvatarUrl,
                            CheckinPhotoUrl = u.CheckinPhotoUrl
                        },
                        Apartments = (from ra in _db.ResidentApartments.AsNoTracking()
                                      where ra.ResidentId == rp.ResidentId
                                      join a in _db.Apartments.AsNoTracking() on ra.ApartmentId equals a.ApartmentId
                                      select new ResidentApartmentDto
                                      {
                                          ResidentApartmentId = ra.ResidentApartmentId,
                                          ApartmentId = ra.ApartmentId,
                                          RelationType = ra.RelationType,
                                          StartDate = ra.StartDate,
                                          EndDate = ra.EndDate,
                                          IsPrimary = ra.IsPrimary,
                                          ApartmentNumber = a.Number
                                      }).ToList()
                    };

            return q;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<object>>> Create([FromForm] CreateResidentRequest dto, CancellationToken ct)
        {
            // ✅ Validate DTO
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value!.Errors)
                    .FirstOrDefault();

                var message = firstError?.ErrorMessage ?? "Validation failed";

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Dữ liệu không hợp lệ"));
            }


            var id = await residentService.CreateResidentAsync(dto, ct);
            return Ok(ApiResponse<object>.SuccessResponse(
                new { resident_id = id },
                    "Tạo cư dân thành công"
                ));
        }

        [HttpPost("add-existing")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<object>>> AddExistingResident([FromForm] AddExistingResidentRequest dto, CancellationToken ct)
        {
            // ✅ Validate DTO
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value!.Errors)
                    .FirstOrDefault();

                var message = firstError?.ErrorMessage ?? "Validation failed";

                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "Dữ liệu không hợp lệ"));
            }

            var id = await residentService.AddExistingResidentToBuildingAsync(dto, ct);
            return Ok(ApiResponse<object>.SuccessResponse(
                new { resident_id = id },
                "Thêm cư dân vào tòa nhà thành công"
            ));
        }

        [HttpGet("paged")]
        public async Task<ActionResult<PagedResult<ResidentDto>>> GetPaged(
            [FromRoute] string schema,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? apartmentId = null,
            [FromQuery] string? q = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDir = null,
            CancellationToken ct = default)
        {
            var paged = await residentService.GetResidentsPagedAsync(schema, pageNumber, pageSize, apartmentId, q, sortBy, sortDir, ct);
            return Ok(paged);
        }
    }
}



using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs.Request.Building;
using SAMS_BE.DTOs.Response;
using SAMS_BE.DTOs.Response.Building;
using SAMS_BE.Infrastructure.Persistence.Global.Models;
using SAMS_BE.Interfaces.IService.GlobalAdmin;
using SAMS_BE.Interfaces.IService.IBuilding;

namespace SAMS_BE.Controllers.Building
{
    [ApiController]
    [Route("api/core/buildings")]
    public sealed class BuildingsController(IBuildingService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<BuildingDto>>>> GetAll(CancellationToken ct)
        {
            var items = await service.GetAllAsync(ct);
            return Ok(ApiResponse<List<BuildingDto>>.SuccessResponse(items.ToList()));
        }

        [HttpGet("all")]
        public async Task<ActionResult<ApiResponse<List<BuildingDto>>>> GetAllIncludingInactive(CancellationToken ct)
        {
            var items = await service.GetAllIncludingInactiveAsync(ct);
            return Ok(ApiResponse<List<BuildingDto>>.SuccessResponse(items.ToList()));
        }

        [HttpGet("dropdown")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<List<BuildingDto>>>> GetAllForDropDown(CancellationToken ct)
        {
            var items = await service.GetAllForDropDownAsync(ct);
            return Ok(ApiResponse<List<BuildingListDropdownDto>>.SuccessResponse(items.ToList()));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<BuildingDto>>> GetById(Guid id, CancellationToken ct)
        {
            try
            {
                var building = await service.GetByIdAsync(id, ct);
                if (building == null)
                {
                    return NotFound(ApiResponse<BuildingDto>.ErrorResponse("Building not found"));
                }
                return Ok(ApiResponse<BuildingDto>.SuccessResponse(building));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<BuildingDto>.ErrorResponse("Internal server error: " + ex.Message));
            }
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<building>>> CreateBuilding([FromForm] CreateBuildingRequest req, CancellationToken ct)
        {
            try
            {
                var created = await service.CreateTenantAsync(req, ct);

                return Ok(ApiResponse<building>.SuccessResponse(created));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<building>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<building>.ErrorResponse("Internal server error: " + ex.Message));
            }
        }

        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<building>>> UpdateBuilding(Guid id, [FromForm] UpdateBuildingRequest req, CancellationToken ct)
        {
            try
            {
                var updated = await service.UpdateBuildingAsync(id, req, ct);
                if (updated == null)
                {
                    return NotFound(ApiResponse<building>.ErrorResponse("Building not found"));
                }
                return Ok(ApiResponse<building>.SuccessResponse(updated));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<building>.ErrorResponse(ex.Message));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<building>.ErrorResponse("Internal server error: " + ex.Message));
            }
        }

    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs.Request.Staff;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.DTOs.Response;
using SAMS_BE.Interfaces.IService.GlobalAdmin;

namespace SAMS_BE.Controllers.GlobalAdmin
{
    [ApiController]
    [Route("api/{schema}/staff")]
    public sealed class StaffController(IStaffService service) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PagedApiResponse<StaffListItemDto>>> Search(
            [FromRoute] string schema,
            [FromQuery] StaffQuery query,
            CancellationToken ct)
        {
            var (items, total, page, size) = await service.SearchAsync(schema, query, ct);
            return Ok(new PagedApiResponse<StaffListItemDto>(items, total, page, size));
        }

        [HttpGet("{staffCode:guid}")]
        public async Task<ActionResult<ApiResponse<StaffDetailDto>>> GetDetail(
            [FromRoute] string schema,
            [FromRoute] Guid staffCode,
            CancellationToken ct)
        {
            var dto = await service.GetDetailAsync(schema, staffCode, ct);
            if (dto is null)
                return NotFound(ApiResponse<StaffDetailDto>.ErrorResponse("Staff not found"));
            return Ok(ApiResponse<StaffDetailDto>.SuccessResponse(dto));
        }

        [HttpPut("{staffCode:guid}")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<object>>> Update(
            [FromRoute] string schema,
            [FromRoute] Guid staffCode,
            [FromForm] StaffUpdateDto dto,
            CancellationToken ct)
        {
            var ok = await service.UpdateAsync(schema, staffCode, dto, ct);
            if (!ok) return NotFound(ApiResponse<object>.ErrorResponse("Staff not found"));
            return Ok(ApiResponse<object>.SuccessResponse(new { ok = true }, "Updated"));
        }

        [HttpPost("{staffCode:guid}/activate")]
        public async Task<ActionResult<ApiResponse<object>>> Activate(
            [FromRoute] string schema,
            [FromRoute] Guid staffCode,
            CancellationToken ct)
        {
            var ok = await service.ActivateAsync(schema, staffCode, ct);
            if (!ok) return NotFound(ApiResponse<object>.ErrorResponse("Staff not found"));
            return Ok(ApiResponse<object>.SuccessResponse(new { ok = true }, "Activated"));
        }

        [HttpPost("{staffCode:guid}/deactivate")]
        public async Task<ActionResult<ApiResponse<object>>> Deactivate(
            [FromRoute] string schema,
            [FromRoute] Guid staffCode,
            [FromQuery] DateTime? date,
            CancellationToken ct)
        {
            var ok = await service.DeactivateAsync(schema, staffCode, date, ct);
            if (!ok) return NotFound(ApiResponse<object>.ErrorResponse("Staff not found"));
            return Ok(ApiResponse<object>.SuccessResponse(new { ok = true }, "Deactivated"));
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<ApiResponse<object>>> Create(
            [FromRoute] string schema,
            [FromForm] StaffCreateRequest dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid)
            {
                var firstError = ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .SelectMany(kvp => kvp.Value!.Errors)
                    .FirstOrDefault();

                var message = firstError?.ErrorMessage ?? "Validation failed";

                return BadRequest(ApiResponse<object>.ErrorResponse(message));
            }

            var id = await service.CreateAsync(schema, dto, ct);
            return CreatedAtAction(nameof(GetDetail), new { schema, staffCode = id },
                ApiResponse<object>.SuccessResponse(new { staff_code = id }, "Created"));
        }
    }
}

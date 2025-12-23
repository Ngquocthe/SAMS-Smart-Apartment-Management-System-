using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs.Response;
using SAMS_BE.DTOs.Response.Staff;
using SAMS_BE.Interfaces.IService.GlobalAdmin;

namespace SAMS_BE.Controllers.GlobalAdmin
{
    [ApiController]
    [Route("api/buildings/{schema}/work-roles")]
    public sealed class WorkRolesController : ControllerBase
    {
        private readonly IWorkRoleService _service;

        public WorkRolesController(IWorkRoleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<WorkRoleOptionDto>>>> GetRoles(
            [FromRoute] string schema,
            [FromQuery] string? search,
            [FromQuery] bool includeInactive = false,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(schema))
                return BadRequest(ApiResponse<List<WorkRoleOptionDto>>.ErrorResponse("Schema is required."));

            var items = await _service.GetRolesAsync(schema, search, includeInactive, ct);
            return Ok(ApiResponse<List<WorkRoleOptionDto>>.SuccessResponse(items.ToList()));
        }
    }
}

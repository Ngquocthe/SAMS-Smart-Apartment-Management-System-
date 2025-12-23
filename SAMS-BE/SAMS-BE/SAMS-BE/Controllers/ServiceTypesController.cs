using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;

namespace SAMS_BE.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class ServiceTypesController : Controller
    {
        private readonly IServiceTypeService _service;
        private readonly ILogger<ServiceTypesController> _logger;

        public ServiceTypesController(IServiceTypeService service, ILogger<ServiceTypesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ServiceTypeListQueryDto query)
        {
            if (query.Page <= 0) query.Page = 1;
            if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 20;
            var result = await _service.ListAsync(query);
            return Ok(result);
        }
        [HttpGet("options")]
        public async Task<IActionResult> Options()
        {
            var items = await _service.GetAllOptionsAsync();
            return Ok(items);
        }

        [HttpPost]
        public async Task<ActionResult<ServiceTypeResponseDto>> Create([FromBody] CreateServiceTypeDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return Created($"/api/ServiceTypes/{result.ServiceTypeId}", result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Validation error when creating service type");
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Conflict when creating service type");
                return Conflict(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error when creating service type");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unexpected server error." });
            }
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceTypeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                if (result == null) return NotFound(new { error = "Service type not found." });
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An unexpected error occurred." });
            }
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ok = await _service.SoftDeleteAsync(id);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPost("{id:guid}/disable")]
        public async Task<IActionResult> Disable(Guid id)
        {
            var ok = await _service.SetActiveAsync(id, false);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpPost("{id:guid}/enable")]
        public async Task<IActionResult> Enable(Guid id)
        {
            var ok = await _service.SetActiveAsync(id, true);
            if (!ok) return NotFound();
            return NoContent();
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListCategories(
        [FromServices] BuildingManagementContext db,
        CancellationToken ct)
        {
            var items = await db.ServiceTypeCategories
                .AsNoTracking()
                .OrderBy(x => x.Name)
                .Select(x => new CategoryDto(
                    x.CategoryId,
                    x.Name,
                    x.Description
                ))
                .ToListAsync(ct);

            return Ok(items);
        }

        [HttpGet("{id:guid}/current-price")]
        [ProducesResponseType(typeof(decimal?), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCurrentPrice(
            Guid id,
            [FromQuery] DateOnly? asOfDate,
            [FromServices] IServicePriceService priceService)
        {
            try
            {
                var price = await priceService.GetCurrentPriceAsync(id, asOfDate);
                return Ok(new { unitPrice = price });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current price for service type {ServiceTypeId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unexpected server error." });
            }
        }

        public record CategoryDto(Guid CategoryId, string Name, string? Description);
    }
}

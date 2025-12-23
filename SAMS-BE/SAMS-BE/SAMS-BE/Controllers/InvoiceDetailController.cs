using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceDetailController : ControllerBase
    {
        private readonly IInvoiceDetailService _service;
        private readonly ILogger<InvoiceDetailController> _logger;

        public InvoiceDetailController(IInvoiceDetailService service, ILogger<InvoiceDetailController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // POST /api/InvoiceDetail
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDetailDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.InvoiceDetailId }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice detail");
                return StatusCode(500, new { error = "An error occurred while creating the invoice detail." });
            }
        }

        // GET /api/InvoiceDetail/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice detail with ID: {DetailId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the invoice detail." });
            }
        }

        // GET /api/InvoiceDetail/invoice/{invoiceId}
        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GetByInvoiceId(Guid invoiceId)
        {
            try
            {
                var result = await _service.GetByInvoiceIdAsync(invoiceId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice details for Invoice {InvoiceId}", invoiceId);
                return StatusCode(500, new { error = "An error occurred while retrieving invoice details." });
            }
        }

        // GET /api/InvoiceDetail?invoiceId=...&serviceId=...&search=...&page=1&pageSize=20&sortBy=ServiceName&sortDir=asc
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] InvoiceDetailListQueryDto query)
        {
            try
            {
                if (query.Page <= 0) query.Page = 1;
                if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 20;

                var result = await _service.ListAsync(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing invoice details");
                return StatusCode(500, new { error = "An error occurred while listing invoice details." });
            }
        }

        // PUT /api/InvoiceDetail/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceDetailDto dto)
        {
            try
            {
                var result = await _service.UpdateAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice detail with ID: {DetailId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the invoice detail." });
            }
        }

        // DELETE /api/InvoiceDetail/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message, error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message, error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice detail with ID: {DetailId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the invoice detail.", error = "An error occurred while deleting the invoice detail." });
            }
        }
    }
}

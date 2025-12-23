using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _service;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(IInvoiceService service, ILogger<InvoiceController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // POST /api/Invoice
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto)
        {
            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.InvoiceId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice");
                return StatusCode(500, new { error = "An error occurred while creating the invoice." });
            }
        }

        // POST /api/Invoice/from-ticket
        // Tạo hóa đơn dựa trên ticketId, unitPrice, quantity, note, serviceTypeId
        [HttpPost("from-ticket")]
        public async Task<IActionResult> CreateFromTicket([FromBody] CreateInvoiceRequest request)
        {
            try
            {
                _logger.LogInformation("Bắt đầu tạo invoice từ ticket. TicketId: {TicketId}, ServiceTypeId: {ServiceTypeId}, UnitPrice: {UnitPrice}",
                    request.TicketId, request.ServiceTypeId, request.UnitPrice);

                var (invoiceId, invoiceNo) = await _service.CreateAsyncInvoice(request);
                _logger.LogInformation("Đã tạo invoice thành công. InvoiceId: {InvoiceId}, InvoiceNo: {InvoiceNo}", invoiceId, invoiceNo);

                var invoice = await _service.GetByIdAsyncInvoice(invoiceId);
                if (invoice is null)
                {
                    _logger.LogWarning("Invoice đã được tạo nhưng không load được. InvoiceId: {InvoiceId}", invoiceId);
                    return StatusCode(500, new { message = "Invoice đã được tạo nhưng không thể load lại. InvoiceId: " + invoiceId });
                }

                _logger.LogInformation("Đã load invoice thành công. InvoiceId: {InvoiceId}", invoiceId);
                return CreatedAtAction(nameof(GetByIdInvoice), new { id = invoiceId }, invoice);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Lỗi validation khi tạo invoice từ ticket. TicketId: {TicketId}", request?.TicketId);
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Lỗi business logic khi tạo invoice từ ticket. TicketId: {TicketId}", request?.TicketId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi tạo invoice từ ticket. TicketId: {TicketId}, Error: {Error}, StackTrace: {StackTrace}",
                    request?.TicketId, ex.Message, ex.StackTrace);
                return StatusCode(500, new { message = $"An error occurred while creating invoice from ticket: {ex.Message}" });
            }
        }

        // GET /api/Invoice/{id}
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
                _logger.LogError(ex, "Error retrieving invoice with ID: {InvoiceId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the invoice." });
            }
        }

        // GET /api/Invoice?apartmentId=...&status=...&search=...&dueFrom=...&dueTo=...&page=1&pageSize=20&sortBy=DueDate&sortDir=desc
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] InvoiceListQueryDto query)
        {
            try
            {
                if (query.Page <= 0) query.Page = 1;
                if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 20;

                var result = await _service.ListAsync(query);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing invoices");
                return StatusCode(500, new { error = "An error occurred while listing invoices." });
            }
        }

        // PUT /api/Invoice/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInvoiceDto dto)
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice with ID: {InvoiceId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the invoice." });
            }
        }

        // PATCH /api/Invoice/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateInvoiceStatusDto dto)
        {
            try
            {
                var result = await _service.UpdateStatusAsync(id, dto);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice status with ID: {InvoiceId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the invoice status." });
            }
        }

        // DELETE /api/Invoice/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return NoContent(); // 204 No Content
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice with ID: {InvoiceId}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the invoice." });
            }
        }

        // POST /api/Invoice/generate-monthly?year=2025&month=1
        [HttpPost("generate-monthly")]
        public async Task<IActionResult> GenerateMonthlyInvoices(
            [FromQuery] int? year,
            [FromQuery] int? month)
        {
            try
            {
                var targetYear = year ?? DateTime.Now.Year;
                var targetMonth = month ?? DateTime.Now.Month;

                var results = await _service.GenerateMonthlyFixedFeeInvoicesAsync(
                    targetYear, targetMonth, User.Identity?.Name ?? "ADMIN");

                return Ok(new
                {
                    message = $"Generated {results.Count} invoices for {targetYear}/{targetMonth}",
                    year = targetYear,
                    month = targetMonth,
                    count = results.Count,
                    invoices = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating monthly invoices");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("by-ticket/{ticketId:guid}")]
        public async Task<IActionResult> GetByTicket(Guid ticketId)
        {
            var items = await _service.GetByTicketAsync(ticketId);
            return Ok(new { items });
        }
        //the
        [HttpGet("by-invoice/{id}")]
        public async Task<IActionResult> GetByIdInvoice(Guid id)
        {
            var invoice = await _service.GetByIdAsyncInvoice(id);
            return invoice == null ? NotFound() : Ok(invoice);
        }

        [HttpPut("detail/{detailId:guid}")]
        public async Task<IActionResult> UpdateDetail(Guid detailId, [FromBody] UpdateInvoiceDetailDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var ok = await _service.UpdateDetailAsync(detailId, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // ✅ NEW: GET /api/Invoice/my?statuses=ISSUED,PAID,OVERDUE (statuses optional, default set in service)
        [HttpGet("my")]
        public async Task<IActionResult> GetMyInvoices([FromQuery] string? statuses = null)
        {
            try
            {
                // Currently service uses default statuses; if future needs, we can parse statuses here and pass down
                var results = await _service.GetMyInvoicesAsync(User);
                return Ok(results);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user's invoices");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // ✅ NEW: GET /api/Invoice/unpaid - Get unpaid invoices for manual receipt creation
        /// <summary>
        /// Get list of unpaid invoices (ISSUED and OVERDUE) for manual receipt creation
        /// </summary>
        [HttpGet("unpaid")]
        public async Task<IActionResult> GetUnpaidInvoices(
            [FromQuery] Guid? apartmentId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page <= 0) page = 1;
                if (pageSize <= 0 || pageSize > 200) pageSize = 20;

                // Query invoices with ISSUED or OVERDUE status
                var query = new InvoiceListQueryDto
                {
                    Status = "ISSUED,OVERDUE", // Only unpaid invoices
                    ApartmentId = apartmentId,
                    Page = page,
                    PageSize = pageSize,
                    SortBy = "DueDate",
                    SortDir = "asc" // Sort by due date ascending (oldest first)
                };

                var result = await _service.ListAsync(query);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unpaid invoices");
                return StatusCode(500, new { error = "An error occurred while getting unpaid invoices." });
            }
        }
    }
}

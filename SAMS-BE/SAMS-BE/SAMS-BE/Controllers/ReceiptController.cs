using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReceiptController : Controller
    {
        private readonly IReceiptService _service;
        private readonly ILogger<ReceiptController> _logger;
        private readonly IReceiptRepository _repository;

        public ReceiptController(
         IReceiptService service,
    ILogger<ReceiptController> logger,
            IReceiptRepository repository)
        {
            _service = service;
            _logger = logger;
            _repository = repository;
        }

        // POST /api/Receipt
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateReceiptDto dto)
        {
            try
            {
                // N?u CreatedBy không ???c cung c?p, l?y t? authenticated user
                if (!dto.CreatedBy.HasValue)
                {
                    // L?y user ID t? JWT token
                    var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("user_id");
                    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                    {
                        dto.CreatedBy = userId;
                    }
                    else
                    {
                        return BadRequest(new { error = "Unable to determine user identity. Please provide CreatedBy or authenticate properly." });
                    }
                }

                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.ReceiptId }, result);
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
                _logger.LogError(ex, "Error creating receipt");
                return StatusCode(500, new { error = "An error occurred while creating the receipt." });
            }
        }

        /// <summary>
        /// T?o Receipt t? ??ng t? payment online (VietQR) thành công
        /// POST /api/Receipt/from-payment
        /// </summary>
        [HttpPost("from-payment")]
        public async Task<IActionResult> CreateFromPayment([FromBody] CreateReceiptFromPaymentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var receipt = await _service.CreateReceiptFromPaymentAsync(
                    dto.InvoiceId,
                    dto.Amount,
                    dto.PaymentMethodCode ?? "VIETQR", // Default to VietQR
                    dto.PaymentDate ?? DateTime.UtcNow,
                    dto.Note
                );

                if (receipt == null)
                {
                    return BadRequest(new { error = "Failed to create receipt. Invoice may not exist or already has a receipt." });
                }

                return CreatedAtAction(nameof(GetById), new { id = receipt.ReceiptId }, receipt);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating receipt from payment");
                return StatusCode(500, new { error = "An error occurred while creating the receipt from payment." });
            }
        }

        // GET /api/Receipt/{id}
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
                _logger.LogError(ex, "Error retrieving receipt with ID: {ReceiptId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the receipt." });
            }
        }

        // ? GET /api/Receipt/invoice/{invoiceId}
        [HttpGet("invoice/{invoiceId}")]
        public async Task<IActionResult> GetByInvoiceId(Guid invoiceId)
        {
            try
            {
                var receipt = await _repository.GetByInvoiceIdAsync(invoiceId);
                if (receipt == null)
                    return NotFound(new { error = $"No receipt found for Invoice {invoiceId}" });

                return Ok(receipt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting receipt for invoice: {InvoiceId}", invoiceId);
                return StatusCode(500, new { error = "An error occurred while retrieving the receipt." });
            }
        }

        // GET /api/Receipt?invoiceId=...&methodId=...&search=...&receivedFrom=...&receivedTo=...&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] ReceiptListQueryDto query)
        {
            try
{
                if (query.Page <= 0) query.Page = 1;
                if (query.PageSize <= 0 || query.PageSize > 200) query.PageSize = 20;

                var (items, total) = await _service.ListAsync(query);
                
                return Ok(new 
                { 
  items = items,
      total = total 
   });
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { error = ex.Message });
 }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error listing receipts");
        return StatusCode(500, new { error = "An error occurred while listing receipts." });
    }
}

        // PUT /api/Receipt/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateReceiptDto dto)
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
                _logger.LogError(ex, "Error updating receipt with ID: {ReceiptId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the receipt." });
            }
        }

        // DELETE /api/Receipt/{id}
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
                _logger.LogError(ex, "Error deleting receipt with ID: {ReceiptId}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the receipt." });
            }
        }
    }
}

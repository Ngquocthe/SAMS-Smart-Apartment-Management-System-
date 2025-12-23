using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IService;

namespace SAMS_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoucherController : Controller
    {
        private readonly Interfaces.IService.IVoucherService _service;
        private readonly IServiceTypeService _serviceTypeService;
        private readonly ILogger<VoucherController> _logger;

        public VoucherController(
          Interfaces.IService.IVoucherService service,
          IServiceTypeService serviceTypeService,
          ILogger<VoucherController> logger)
        {
            _service = service;
            _serviceTypeService = serviceTypeService;
            _logger = logger;
        }

        // POST /api/Voucher
        // ✅ Manual creation only (giống Invoice) – auto detect Ticket dùng endpoint riêng
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateVoucherDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.VoucherId }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher");
                return StatusCode(500, new { error = "An error occurred while creating the voucher." });
            }
        }

        // GET /api/Voucher/{id}
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
                _logger.LogError(ex, "Error retrieving voucher with ID: {VoucherId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the voucher." });
            }
        }

        // GET /api/Voucher?apartmentId=...&type=...&status=...&search=...&dateFrom=...&dateTo=...&page=1&pageSize=20&sortBy=Date&sortDir=desc
        // Note: Type filter is optional and deprecated since Voucher is always PAYMENT
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] VoucherListQueryDto query)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
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
                _logger.LogError(ex, "Error listing vouchers");
                return StatusCode(500, new { error = "An error occurred while listing vouchers." });
            }
        }

        // PUT /api/Voucher/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVoucherDto dto)
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
                _logger.LogError(ex, "Error updating voucher with ID: {VoucherId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the voucher." });
            }
        }

        // PATCH /api/Voucher/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateVoucherStatusDto dto)
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
                _logger.LogError(ex, "Error updating voucher status with ID: {VoucherId}", id);
                return StatusCode(500, new { error = "An error occurred while updating the voucher status." });
            }
        }

        // DELETE /api/Voucher/{id}
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
                _logger.LogError(ex, "Error deleting voucher with ID: {VoucherId}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the voucher." });
            }
        }

        [HttpPost("from-ticket")]
        public async Task<IActionResult> CreateFromTicket([FromBody] CreateVoucherRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                if (!ValidateTicketVoucherPayload(request, out var payloadError))
                    return BadRequest(new { message = payloadError });

                var (id, number) = await _service.CreateFromTicketAsync(request);
                return Ok(new { voucherId = id, voucherNumber = number });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher from ticket");
                return StatusCode(500, new { error = "An error occurred while creating the voucher." });
            }
        }

        [HttpPost("from-maintenance")]
        public async Task<IActionResult> CreateFromMaintenance([FromBody] CreateVoucherFromMaintenanceRequest request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var (id, number) = await _service.CreateFromMaintenanceAsync(request);
                return Ok(new
                {
                    success = true,
                    message = $"Tạo phiếu chi thành công. Số chứng từ: {number}",
                    voucherId = id,
                    voucherNumber = number
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating voucher from maintenance history");
                return StatusCode(500, new { success = false, error = "An error occurred while creating the voucher." });
            }
        }

        [HttpGet("by-ticket/{ticketId:guid}")]
        public async Task<IActionResult> GetByTicket(Guid ticketId)
        {
            var items = await _service.GetByTicketAsync(ticketId);
            return Ok(new { items });
        }

        [HttpGet("default-maintenance-service-type")]
        public async Task<IActionResult> GetDefaultMaintenanceServiceType()
        {
            try
            {
                var defaultServiceType = await _service.GetDefaultMaintenanceServiceTypeAsync();
                if (defaultServiceType == null)
                {
                    return Ok(new { id = (Guid?)null, name = (string?)null });
                }
                return Ok(new { id = defaultServiceType.Value.Id, name = defaultServiceType.Value.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting default maintenance service type");
                return StatusCode(500, new { error = "An error occurred while getting default service type." });
            }
        }

        [HttpGet("service-types")]
        public async Task<IActionResult> GetServiceTypes()
        {
            try
            {
                var serviceTypes = await _serviceTypeService.GetAllOptionsAsync();
                var result = serviceTypes.Select(st => new
                {
                    id = st.Value,
                    serviceTypeId = st.Value,
                    name = st.Label,
                    serviceTypeName = st.Label
                }).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting service types");
                return StatusCode(500, new { error = "An error occurred while getting service types." });
            }
        }

        [HttpGet("by-history/{historyId:guid}")]
        public async Task<IActionResult> GetByHistory(Guid historyId)
        {
            var voucherId = await _service.GetVoucherIdByHistoryAsync(historyId);
            var defaultServiceType = await _service.GetDefaultMaintenanceServiceTypeAsync();
            var defaultServiceTypePayload = defaultServiceType.HasValue
              ? new { id = defaultServiceType.Value.Id, name = defaultServiceType.Value.Name }
              : null;
            if (voucherId == null)
            {
                return Ok(new { hasVoucher = false, voucherId = (Guid?)null, defaultServiceType = defaultServiceTypePayload });
            }
            var voucher = await _service.GetByIdAsync(voucherId.Value);
            return Ok(new { hasVoucher = true, voucherId = voucherId, voucher = voucher, defaultServiceType = defaultServiceTypePayload });
        }

        [HttpPut("item/{itemId:guid}")]
        public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateVoucherItemDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var ok = await _service.UpdateItemAsync(itemId, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        private static bool ValidateTicketVoucherPayload(CreateVoucherRequest request, out string errorMessage)
        {
            var hasAmount = request.Amount > 0;
            var hasQuantity = request.Quantity.HasValue && request.Quantity.Value > 0;
            var hasUnitPrice = request.UnitPrice.HasValue && request.UnitPrice.Value > 0;

            if (hasAmount || (hasQuantity && hasUnitPrice))
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = "Amount phải lớn hơn 0 hoặc cung cấp Quantity & UnitPrice hợp lệ.";
            return false;
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IService;
using System.Security.Claims;

namespace SAMS_BE.Controllers;

[ApiController]
[Route("api/invoice-configuration")]
[Authorize]
public class InvoiceConfigurationController : ControllerBase
{
    private readonly IInvoiceConfigurationService _service;
    private readonly IInvoiceService _invoiceService;
    private readonly ILogger<InvoiceConfigurationController> _logger;

    public InvoiceConfigurationController(
        IInvoiceConfigurationService service,
        IInvoiceService invoiceService,
        ILogger<InvoiceConfigurationController> logger)
    {
        _service = service;
        _invoiceService = invoiceService;
        _logger = logger;
    }

    /// <summary>
    /// Lấy cấu hình tự động tạo invoice hiện tại
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "accountant,admin,manager")]
    public async Task<ActionResult<InvoiceConfigurationResponseDto>> GetCurrentConfig()
    {
        try
        {
            var config = await _service.GetCurrentConfigAsync();
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice configuration");
            return StatusCode(500, new { message = "Lỗi khi lấy cấu hình tự động tạo invoice", error = ex.Message });
        }
    }

    /// <summary>
    /// Tạo hoặc cập nhật cấu hình tự động tạo invoice (chỉ Accountant và Admin)
    /// </summary>
    [HttpPut]
    [Authorize(Roles = "accountant,admin")]
    public async Task<ActionResult<InvoiceConfigurationResponseDto>> CreateOrUpdateConfig(
        [FromBody] CreateOrUpdateInvoiceConfigDto dto)
    {
        if (!ModelState.IsValid)
        {
      return BadRequest(ModelState);
        }

        try
        {
            var username = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("preferred_username")?.Value
                ?? "SYSTEM";

            var config = await _service.CreateOrUpdateAsync(dto, username);

            return Ok(new
            {
                message = "Cập nhật cấu hình thành công",
                data = config
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating invoice configuration");
            return StatusCode(500, new { message = "Lỗi khi cập nhật cấu hình", error = ex.Message });
        }
    }

    /// <summary>
    /// Chạy thử auto tạo hóa đơn theo cấu hình hiện tại (dùng cho FE test)
    /// </summary>
    [HttpPost("run-once")]
    [Authorize(Roles = "accountant,admin")]
    public async Task<IActionResult> RunAutoInvoiceOnce(
        [FromQuery] int? year,
        [FromQuery] int? month)
    {
        try
        {
            var targetYear = year ?? DateTime.Now.Year;
            var targetMonth = month ?? DateTime.Now.Month;

            var username = User.Identity?.Name
                ?? User.FindFirst("preferred_username")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? "ADMIN";

            var invoices = await _invoiceService.GenerateMonthlyFixedFeeInvoicesAsync(
                targetYear,
                targetMonth,
                username);

            return Ok(new
            {
                message = $"Đã chạy auto tạo hóa đơn cho {targetYear}/{targetMonth}.",
                year = targetYear,
                month = targetMonth,
                count = invoices.Count,
                invoices
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running auto invoice generation once");
            return StatusCode(500, new { message = "Lỗi khi chạy auto tạo hóa đơn", error = ex.Message });
        }
    }
}

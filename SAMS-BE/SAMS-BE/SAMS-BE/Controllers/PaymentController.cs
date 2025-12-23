using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SAMS_BE.DTOs;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Services;

namespace SAMS_BE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IInvoicePaymentService _invoicePaymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentService paymentService,
            IInvoicePaymentService invoicePaymentService,
            ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _invoicePaymentService = invoicePaymentService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo mã QR thanh toán SePay
        /// </summary>
        /// <param name="request">Thông tin thanh toán</param>
        /// <returns>Thông tin mã QR thanh toán</returns>
        [HttpPost("create")]
        public async Task<ActionResult<PaymentResponseDto>> CreatePaymentLink([FromBody] CreatePaymentRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    // Lấy tất cả lỗi validation
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();

                    var errorMessage = errors.Any() 
                        ? string.Join("; ", errors) 
                        : "Dữ liệu đầu vào không hợp lệ";

                    return BadRequest(new PaymentResponseDto
                    {
                        Success = false,
                        Message = errorMessage
                    });
                }

                var result = await _paymentService.CreatePaymentLinkAsync(request);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreatePaymentLink");
                return StatusCode(500, new PaymentResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái thanh toán SePay (dùng uniqueCode và amount)
        /// </summary>
        /// <param name="uniqueCode">Mã unique từ QR code</param>
        /// <param name="amount">Số tiền thanh toán</param>
        /// <returns>Trạng thái thanh toán</returns>
        [HttpGet("status/{uniqueCode}")]
        public async Task<ActionResult<PaymentStatusDto>> GetPaymentStatus(string uniqueCode, [FromQuery] int amount)
        {
            try
            {
                if (amount <= 0)
                {
                    return BadRequest(new PaymentStatusDto
                    {
                        Success = false,
                        Message = "Amount là bắt buộc và phải lớn hơn 0",
                        Status = "ERROR"
                    });
                }

                var paymentService = _paymentService as PaymentService;
                if (paymentService == null)
                {
                    return StatusCode(500, new PaymentStatusDto
                    {
                        Success = false,
                        Message = "Payment service không hợp lệ",
                        Status = "ERROR"
                    });
                }

                var result = await paymentService.GetPaymentStatusByUniqueCodeAsync(uniqueCode, amount);
                
                if (result.Success)
                {
                    return Ok(result);
                }
                
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting payment status for uniqueCode: {uniqueCode}");
                return StatusCode(500, new PaymentStatusDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    Status = "ERROR"
                });
            }
        }

        #region Invoice Payment

        /// <summary>
        /// Tạo QR code thanh toán cho hóa đơn
        /// </summary>
        /// <param name="dto">Thông tin invoice payment</param>
        /// <returns>QR code và thông tin hóa đơn</returns>
        [HttpPost("invoice/create")]
        [Authorize]
        public async Task<ActionResult<InvoicePaymentResponseDto>> CreateInvoicePayment([FromBody] CreateInvoicePaymentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();

                    return BadRequest(new InvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = string.Join("; ", errors)
                    });
                }

                var result = await _invoicePaymentService.CreateInvoicePaymentAsync(dto);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice payment");
                return StatusCode(500, new InvoicePaymentResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Verify payment status và tự động tạo Receipt + gửi email nếu đã thanh toán
        /// </summary>
        /// <param name="dto">Invoice ID và Order Code</param>
        /// <returns>Trạng thái thanh toán và thông tin receipt</returns>
        [HttpPost("invoice/verify")]
        [Authorize]
        public async Task<ActionResult<VerifyInvoicePaymentResponseDto>> VerifyInvoicePayment([FromBody] VerifyInvoicePaymentDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value!.Errors.Select(e => e.ErrorMessage))
                        .ToList();

                    return BadRequest(new VerifyInvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = string.Join("; ", errors),
                        IsPaid = false
                    });
                }

                var result = await _invoicePaymentService.VerifyInvoicePaymentAsync(dto);

                if (result.Success)
                {
                    return Ok(result);
                }

                return BadRequest(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying invoice payment");
                return StatusCode(500, new VerifyInvoicePaymentResponseDto
                {
                    Success = false,
                    Message = $"Lỗi server: {ex.Message}",
                    IsPaid = false
                });
            }
        }

        #endregion
    }
}
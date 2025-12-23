using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces;
using SAMS_BE.Interfaces.IMail;
using SAMS_BE.Interfaces.IService;
using SAMS_BE.Models;
using SAMS_BE.Services;
using System.Text;

namespace SAMS_BE.Services
{
    public class InvoicePaymentService : IInvoicePaymentService
    {
        private readonly BuildingManagementContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IReceiptService _receiptService;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<InvoicePaymentService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InvoicePaymentService(
            BuildingManagementContext context,
            IPaymentService paymentService,
            IReceiptService receiptService,
            IEmailSender emailSender,
            ILogger<InvoicePaymentService> logger,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _paymentService = paymentService;
            _receiptService = receiptService;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<InvoicePaymentResponseDto> CreateInvoicePaymentAsync(CreateInvoicePaymentDto dto)
        {
            try
            {
                // 1. Lấy invoice với details
                var invoice = await _context.Invoices
                    .Include(i => i.Apartment)
                    .Include(i => i.InvoiceDetails)
                    .FirstOrDefaultAsync(i => i.InvoiceId == dto.InvoiceId);

                if (invoice == null)
                {
                    return new InvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = "Hóa đơn không tồn tại"
                    };
                }

                // 2. Validate invoice status
                if (invoice.Status == "PAID")
                {
                    return new InvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = "Hóa đơn đã được thanh toán"
                    };
                }

                if (invoice.Status != "ISSUED")
                {
                    return new InvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = $"Không thể thanh toán hóa đơn có trạng thái: {invoice.Status}"
                    };
                }

                // 3. Check overdue
                var today = DateOnly.FromDateTime(DateTimeHelper.VietnamNow);
                if (invoice.DueDate < today)
                {
                    return new InvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = "Hóa đơn đã quá hạn thanh toán"
                    };
                }

                // 4. Validate minimum amount (SePay yêu cầu tối thiểu 2000 VNĐ)
                if (invoice.TotalAmount < 2000)
                {
                    return new InvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = $"Số tiền hóa đơn phải từ 2,000 VNĐ trở lên để tạo mã QR thanh toán. Số tiền hiện tại: {invoice.TotalAmount:N0} VNĐ"
                    };
                }

                // 5. Tạo payment request - PaymentService sẽ tự tạo OrderCode (GUID)
                var paymentRequest = new CreatePaymentRequestDto
                {
                    OrderCode = GenerateOrderCode(),
                    Amount = (int)invoice.TotalAmount,
                    Description = $"Thanh toán hóa đơn {invoice.InvoiceNo}", // Mô tả cho user, không dùng để check
                    ReturnUrl = dto.ReturnUrl,
                    CancelUrl = dto.CancelUrl,
                    ExpiredAt = 30 // 30 phút
                };

                var paymentResult = await _paymentService.CreatePaymentLinkAsync(paymentRequest);

                if (!paymentResult.Success)
                {
                    return new InvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = $"Lỗi tạo QR code: {paymentResult.Message}"
                    };
                }

                // Lưu OrderCode để verify sau này
                var orderCodeToVerify = paymentResult.OrderCode;

                _logger.LogInformation("Created payment QR for invoice {InvoiceNo}, OrderCode: {OrderCode}",
                    invoice.InvoiceNo, orderCodeToVerify);

                // 5. Map invoice details
                var invoiceItems = invoice.InvoiceDetails.Select(d => new InvoiceItemDto
                {
                    Description = d.Description ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount ?? 0m
                }).ToList();

                return new InvoicePaymentResponseDto
                {
                    Success = true,
                    Message = "Tạo QR code thành công",
                    Invoice = new InvoiceInfoDto
                    {
                        InvoiceId = invoice.InvoiceId,
                        InvoiceNo = invoice.InvoiceNo,
                        TotalAmount = invoice.TotalAmount,
                        Status = invoice.Status,
                        DueDate = invoice.DueDate,
                        ApartmentNumber = invoice.Apartment?.Number,
                        Items = invoiceItems
                    },
                    QrCode = paymentResult.QrCode,
                    CheckoutUrl = paymentResult.CheckoutUrl,
                    OrderCode = orderCodeToVerify // Trả về hex string
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice payment");
                return new InvoicePaymentResponseDto
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.Message}"
                };
            }
        }

        public async Task<VerifyInvoicePaymentResponseDto> VerifyInvoicePaymentAsync(VerifyInvoicePaymentDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy invoice
                var invoice = await _context.Invoices
             .Include(i => i.Apartment)
                  .ThenInclude(a => a.ResidentApartments)
             .ThenInclude(ra => ra.Resident)
                 .ThenInclude(r => r.User)
                        .Include(i => i.InvoiceDetails)
                       .FirstOrDefaultAsync(i => i.InvoiceId == dto.InvoiceId);

                if (invoice == null)
                {
                    return new VerifyInvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = "Hóa đơn không tồn tại",
                        IsPaid = false
                    };
                }

                // 2. Nếu đã PAID rồi, trả về thông tin (kiểm tra cả receipt)
                if (invoice.Status == "PAID")
                {
                    // Lấy receipt nếu có
                    var existingReceipt = await _context.Receipts
           .FirstOrDefaultAsync(r => r.InvoiceId == invoice.InvoiceId);

                    return new VerifyInvoicePaymentResponseDto
                    {
                        Success = true,
                        Message = "Hóa đơn đã được thanh toán trước đó",
                        IsPaid = true,
                        Invoice = MapInvoiceInfo(invoice),
                        Receipt = existingReceipt != null ? new ReceiptInfoDto
                        {
                            ReceiptId = existingReceipt.ReceiptId,
                            ReceiptNo = existingReceipt.ReceiptNo,
                            ReceivedDate = existingReceipt.ReceivedDate,
                            AmountTotal = existingReceipt.AmountTotal
                        } : null
                    };
                }

                // 3. Check payment status từ SePay - Dùng InvoiceNo để tìm transaction
                var paymentService = _paymentService as PaymentService;
                if (paymentService == null)
                {
                    return new VerifyInvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = "Payment service không hợp lệ",
                        IsPaid = false
                    };
                }

                // Tìm transaction theo OrderCode (GUID từ PaymentService)
                var orderCodeString = dto.OrderCode; // Hex string: "5692CE29FC064433"

                _logger.LogInformation("Verifying payment for invoice {InvoiceNo}, OrderCode: {OrderCode}, expectedAmount: {Amount}",
             invoice.InvoiceNo, orderCodeString, invoice.TotalAmount);

                var paymentStatus = await paymentService.GetPaymentStatusByUniqueCodeAsync(
          orderCodeString, // SePay sẽ tìm "Thanhtoanorder5692CE29FC064433"
           (int)invoice.TotalAmount
                   );

                _logger.LogInformation("Payment status result: Success={Success}, Status={Status}, Message={Message}",
      paymentStatus.Success, paymentStatus.Status, paymentStatus.Message);

                // 4. Nếu chưa thanh toán
                if (!paymentStatus.Success || paymentStatus.Status != "PAID")
                {
                    return new VerifyInvoicePaymentResponseDto
                    {
                        Success = true,
                        Message = $"Chưa nhận được thanh toán. Status: {paymentStatus.Status}",
                        IsPaid = false,
                        Invoice = MapInvoiceInfo(invoice)
                    };
                }

                // 5. Đã thanh toán → Tạo Receipt tự động (ReceiptService sẽ tự động tạo Journal Entry)
                _logger.LogInformation("Payment confirmed for invoice {InvoiceNo}, creating receipt...", invoice.InvoiceNo);

                // Parse payment date từ SePay response
                DateTime paymentDate = DateTimeHelper.VietnamNow;
                if (paymentStatus.Data?.TransactionDateTime != null &&
            DateTime.TryParse(paymentStatus.Data.TransactionDateTime, out var parsedDate))
                {
                    paymentDate = parsedDate;
                }

                // ✅ Detach invoice entity trước khi gọi child method để tránh tracking conflict
                _context.Entry(invoice).State = EntityState.Detached;

                Receipt? receipt = null;
                try
                {
                    // Tạo Receipt - ReceiptService sẽ tạo Receipt, update Invoice, tạo Journal Entry
                    receipt = await _receiptService.CreateReceiptFromPaymentAsync(
                        invoiceId: invoice.InvoiceId,
              amount: invoice.TotalAmount,
                  paymentMethodCode: "SEPAY",
              paymentDate: paymentDate,
        note: $"Thanh toán online qua SePay - Ref: {paymentStatus.Data?.Reference ?? orderCodeString}"
           );
                }
                catch (DbUpdateException dbEx)
                {
                    // ✅ Log chi tiết DB error
                    _logger.LogError(dbEx, "Database error creating receipt for invoice {InvoiceNo}. Inner exception: {InnerException}",
                           invoice.InvoiceNo, dbEx.InnerException?.Message);

                    await transaction.RollbackAsync();

                    return new VerifyInvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = $"Lỗi database khi tạo phiếu thu: {dbEx.InnerException?.Message ?? dbEx.Message}",
                        IsPaid = false
                    };
                }
                catch (InvalidOperationException ioEx)
                {
                    // ✅ Log validation error
                    _logger.LogError(ioEx, "Validation error creating receipt for invoice {InvoiceNo}", invoice.InvoiceNo);

                    await transaction.RollbackAsync();

                    return new VerifyInvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = $"Lỗi validation: {ioEx.Message}",
                        IsPaid = false
                    };
                }

                if (receipt == null)
                {
                    _logger.LogError("Failed to create receipt for invoice {InvoiceNo} - receipt is null", invoice.InvoiceNo);
                    await transaction.RollbackAsync();

                    return new VerifyInvoicePaymentResponseDto
                    {
                        Success = false,
                        Message = "Lỗi tạo phiếu thu sau khi thanh toán thành công",
                        IsPaid = false
                    };
                }

                _logger.LogInformation("Receipt {ReceiptNo} created successfully for invoice {InvoiceNo}",
                    receipt.ReceiptNo, invoice.InvoiceNo);

                // 6. Gửi email xác nhận
                try
                {
                    await SendPaymentConfirmationEmailAsync(invoice, paymentStatus.Data);
                }
                catch (Exception emailEx)
                {
                    // ✅ Email fail không làm transaction fail
                    _logger.LogError(emailEx, "Failed to send payment confirmation email for invoice {InvoiceNo}", invoice.InvoiceNo);
                }

                await transaction.CommitAsync();

                _logger.LogInformation("Invoice {InvoiceNo} paid successfully via SePay with receipt {ReceiptNo}",
               invoice.InvoiceNo, receipt.ReceiptNo);

                return new VerifyInvoicePaymentResponseDto
                {
                    Success = true,
                    Message = "Thanh toán thành công",
                    IsPaid = true,
                    Invoice = MapInvoiceInfo(invoice),
                    Receipt = new ReceiptInfoDto
                    {
                        ReceiptId = receipt.ReceiptId,
                        ReceiptNo = receipt.ReceiptNo,
                        ReceivedDate = receipt.ReceivedDate,
                        AmountTotal = receipt.AmountTotal
                    },
                    Transaction = new PaymentTransactionDto
                    {
                        TransactionReference = paymentStatus.Data?.Reference,
                        PaymentTime = paymentDate,
                        Amount = invoice.TotalAmount
                    }
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                // ✅ Log full exception stack
                _logger.LogError(ex, "Error verifying invoice payment for InvoiceId {InvoiceId}. Inner: {InnerException}",
                   dto.InvoiceId, ex.InnerException?.Message);

                return new VerifyInvoicePaymentResponseDto
                {
                    Success = false,
                    Message = $"Lỗi hệ thống: {ex.InnerException?.Message ?? ex.Message}",
                    IsPaid = false
                };
            }
        }
        private InvoiceInfoDto MapInvoiceInfo(Invoice invoice)
        {
            return new InvoiceInfoDto
            {
                InvoiceId = invoice.InvoiceId,
                InvoiceNo = invoice.InvoiceNo,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                DueDate = invoice.DueDate,
                ApartmentNumber = invoice.Apartment?.Number,
                Items = invoice.InvoiceDetails?.Select(d => new InvoiceItemDto
                {
                    Description = d.Description ?? "",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Amount = d.Amount ?? 0m
                }).ToList()
            };
        }

        private async Task SendPaymentConfirmationEmailAsync(Invoice invoice, PaymentDataDto? paymentData)
        {
            try
            {
                // Lấy userId từ claims (user đang thanh toán)
                var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Cannot get current user ID for invoice {InvoiceNo}", invoice.InvoiceNo);
                    return;
                }

                // Lấy thông tin user và resident (giống AmenityBookingService)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                var residentProfile = await _context.ResidentProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(rp => rp.UserId == userId);

                string? residentEmail = null;
                string residentName = "Quý khách";

                // Ưu tiên email từ ResidentProfile
                if (residentProfile != null)
                {
                    residentEmail = residentProfile.Email;
                    residentName = residentProfile.FullName ?? residentName;
                }
                // Fallback sang User nếu không có ResidentProfile hoặc không có email
                else if (user != null)
                {
                    residentEmail = user.Email;
                    residentName = $"{user.FirstName} {user.LastName}".Trim();
                    if (string.IsNullOrWhiteSpace(residentName))
                    {
                        residentName = user.Username ?? "Quý khách";
                    }
                }

                // Chỉ gửi email nếu tìm được địa chỉ email
                if (string.IsNullOrWhiteSpace(residentEmail))
                {
                    _logger.LogWarning("No email found for user {UserId} paying invoice {InvoiceNo}",
                        userId, invoice.InvoiceNo);
                    return;
                }

                _logger.LogInformation("Sending payment confirmation email to {Email} (Name: {Name}) for invoice {InvoiceNo}",
                    residentEmail, residentName, invoice.InvoiceNo);

                // Load template
                var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "InvoicePaymentSuccessEmail.html");
                var template = await System.IO.File.ReadAllTextAsync(templatePath);

                // Replace placeholders
                var emailBody = template
                    .Replace("{{CompanyName}}", "NOAH")
                    .Replace("{{AppName}}", "NOAH Building Management")
                    .Replace("{{SupportEmail}}", "support@noahbuilding.me")
                    .Replace("{{ResidentName}}", residentName)
                    .Replace("{{InvoiceNo}}", invoice.InvoiceNo)
                    .Replace("{{ApartmentNumber}}", invoice.Apartment?.Number ?? "")
                    .Replace("{{IssueDate}}", invoice.IssueDate.ToString("dd/MM/yyyy"))
                    .Replace("{{DueDate}}", invoice.DueDate.ToString("dd/MM/yyyy"))
                    .Replace("{{TotalAmount}}", $"{invoice.TotalAmount:N0} đ")
                    .Replace("{{PaymentTime}}", DateTimeHelper.VietnamNow.ToString("dd/MM/yyyy HH:mm:ss"));

                await _emailSender.SendEmailAsync(
                    residentEmail,
                    $"Xác nhận thanh toán hóa đơn {invoice.InvoiceNo}",
                    emailBody
                );

                _logger.LogInformation("Payment confirmation email sent to {Email} for invoice {InvoiceNo}",
                    residentEmail, invoice.InvoiceNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment confirmation email for invoice {InvoiceNo}", invoice.InvoiceNo);
                // Không throw exception, email fail không nên block payment
            }
        }

        private int GenerateOrderCode()
        {
            // Generate unique order code (timestamp-based)
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = new Random().Next(1000, 9999);
            return (int)(timestamp % 1000000000) + random;
        }
    }
}

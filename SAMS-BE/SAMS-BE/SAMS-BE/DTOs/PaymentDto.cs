using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreatePaymentRequestDto
    {
        [Required(ErrorMessage = "OrderCode là bắt buộc")]
        public int OrderCode { get; set; }

        [Required(ErrorMessage = "Amount là bắt buộc")]
        [Range(2000, int.MaxValue, ErrorMessage = "Số tiền phải từ 2,000 VNĐ trở lên")]
        public int Amount { get; set; }

        [Required(ErrorMessage = "Description là bắt buộc")]
        [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "BuyerName không được vượt quá 100 ký tự")]
        public string? BuyerName { get; set; }

        [MaxLength(15, ErrorMessage = "BuyerPhone không được vượt quá 15 ký tự")]
        public string? BuyerPhone { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(100, ErrorMessage = "BuyerEmail không được vượt quá 100 ký tự")]
        public string? BuyerEmail { get; set; }

        [MaxLength(255, ErrorMessage = "ReturnUrl không được vượt quá 255 ký tự")]
        public string? ReturnUrl { get; set; }

        [MaxLength(255, ErrorMessage = "CancelUrl không được vượt quá 255 ký tự")]
        public string? CancelUrl { get; set; }

        // Thời gian hết hạn (phút), mặc định 15 phút
        [Range(1, 1440, ErrorMessage = "ExpiredAt phải từ 1 đến 1440 phút")]
        public int ExpiredAt { get; set; } = 15;

        // Metadata bổ sung
        public Dictionary<string, object>? Metadata { get; set; }

        // Danh sách mặt hàng (tùy chọn)
        public List<PaymentItemDto>? Items { get; set; }
    }

    public class PaymentItemDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(1, int.MaxValue)]
        public int Price { get; set; }
    }

    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? CheckoutUrl { get; set; }
        public string? OrderCode { get; set; }
        public string? PaymentLinkId { get; set; }
        public string? QrCode { get; set; }
    }

    public class PaymentWebhookDto
    {
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public bool Success { get; set; }
        public PaymentDataDto? Data { get; set; }
        public string? Signature { get; set; }
    }

    public class PaymentDataDto
    {
        public string OrderCode { get; set; } = string.Empty;
        public int Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string TransactionDateTime { get; set; } = string.Empty;
        public string Currency { get; set; } = "VND";
        public string PaymentLinkId { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public string CounterAccountBankId { get; set; } = string.Empty;
        public string CounterAccountBankName { get; set; } = string.Empty;
        public string CounterAccountName { get; set; } = string.Empty;
        public string CounterAccountNumber { get; set; } = string.Empty;
        public string VirtualAccountName { get; set; } = string.Empty;
        public string VirtualAccountNumber { get; set; } = string.Empty;
    }

    public class PaymentStatusDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public PaymentDataDto? Data { get; set; }
    }

    public class CancelPaymentRequestDto
    {
        public string? CancellationReason { get; set; }
    }

    public class CancelPaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? OrderCode { get; set; }
    }
}
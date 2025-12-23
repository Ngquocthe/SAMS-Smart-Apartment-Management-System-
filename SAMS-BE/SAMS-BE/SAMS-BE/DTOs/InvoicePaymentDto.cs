using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    /// <summary>
    /// DTO request để tạo QR thanh toán cho invoice
    /// </summary>
    public class CreateInvoicePaymentDto
    {
        [Required(ErrorMessage = "InvoiceId là bắt buộc")]
        public Guid InvoiceId { get; set; }

        /// <summary>
        /// URL redirect sau khi thanh toán thành công
        /// </summary>
        public string? ReturnUrl { get; set; }

        /// <summary>
        /// URL redirect khi hủy thanh toán
        /// </summary>
        public string? CancelUrl { get; set; }
    }

    /// <summary>
    /// DTO response khi tạo QR thanh toán invoice
    /// </summary>
    public class InvoicePaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Thông tin hóa đơn
        /// </summary>
        public InvoiceInfoDto? Invoice { get; set; }
        
        /// <summary>
        /// Link QR code
        /// </summary>
        public string? QrCode { get; set; }
        
        /// <summary>
        /// Link checkout
        /// </summary>
        public string? CheckoutUrl { get; set; }
        
        /// <summary>
        /// Order code để check status (hex string từ PaymentService)
        /// </summary>
        public string OrderCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Thông tin invoice trong response
    /// </summary>
    public class InvoiceInfoDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNo { get; set; } = null!;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public DateOnly DueDate { get; set; }
        public string? ApartmentNumber { get; set; }
        public List<InvoiceItemDto>? Items { get; set; }
    }

    /// <summary>
    /// Chi tiết item trong invoice
    /// </summary>
    public class InvoiceItemDto
    {
        public string Description { get; set; } = null!;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// DTO request để verify payment invoice
    /// </summary>
    public class VerifyInvoicePaymentDto
    {
        [Required(ErrorMessage = "InvoiceId là bắt buộc")]
        public Guid InvoiceId { get; set; }

        [Required(ErrorMessage = "OrderCode là bắt buộc")]
        public string OrderCode { get; set; } = null!;
    }

    /// <summary>
    /// DTO response khi verify payment invoice
    /// </summary>
    public class VerifyInvoicePaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsPaid { get; set; }
        
        /// <summary>
        /// Thông tin invoice sau khi verify
        /// </summary>
        public InvoiceInfoDto? Invoice { get; set; }
        
        /// <summary>
        /// Thông tin receipt (nếu đã thanh toán)
        /// </summary>
        public ReceiptInfoDto? Receipt { get; set; }
        
        /// <summary>
        /// Thông tin giao dịch
        /// </summary>
        public PaymentTransactionDto? Transaction { get; set; }
    }

    /// <summary>
    /// Thông tin receipt
    /// </summary>
    public class ReceiptInfoDto
    {
        public Guid ReceiptId { get; set; }
        public string ReceiptNo { get; set; } = null!;
        public decimal AmountTotal { get; set; }
        public DateTime ReceivedDate { get; set; }
    }

    /// <summary>
    /// Thông tin giao dịch thanh toán
    /// </summary>
    public class PaymentTransactionDto
    {
        public string? TransactionReference { get; set; }
        public DateTime? PaymentTime { get; set; }
        public decimal Amount { get; set; }
    }
}

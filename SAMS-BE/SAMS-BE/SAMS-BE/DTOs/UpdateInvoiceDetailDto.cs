using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class UpdateInvoiceDetailDto
    {
        public Guid? ServiceId { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [Range(0.000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal? Quantity { get; set; }

        // UnitPrice được snapshot tự động từ ServicePrice khi đổi ServiceId, không cần gửi khi update thông thường
        [Range(0.000001, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal? UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
        public decimal? VatRate { get; set; }

        public Guid? TicketId { get; set; }
    }
}

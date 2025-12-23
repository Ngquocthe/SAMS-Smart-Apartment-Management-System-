using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateInvoiceDetailDto
    {
        [Required]
        public Guid InvoiceId { get; set; }

        [Required]
        public Guid ServiceId { get; set; }

        [StringLength(255)]
        public string? Description { get; set; }

        [Required]
        [Range(0.000001, double.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public decimal Quantity { get; set; }

        [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
        public decimal? VatRate { get; set; }

        public Guid? TicketId { get; set; }
    }
}

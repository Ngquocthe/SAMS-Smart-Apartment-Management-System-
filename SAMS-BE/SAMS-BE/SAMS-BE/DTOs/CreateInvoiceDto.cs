using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateInvoiceDto
    {
        [Required, StringLength(64)]
        public string InvoiceNo { get; set; } = null!;
        [Required]
        public Guid ApartmentId { get; set; }
        [Required]
        public DateOnly IssueDate { get; set; }
        [Required]
        public DateOnly DueDate { get; set; }
        [Required, StringLength(64)]
        public string Status { get; set; } = "DRAFT";
        [StringLength(1000)]
        public string? Note { get; set; }

        // Optional: người tạo (để ghi log vào ticket nếu có TicketId)
        public Guid? CreatedByUserId { get; set; }
    }
}

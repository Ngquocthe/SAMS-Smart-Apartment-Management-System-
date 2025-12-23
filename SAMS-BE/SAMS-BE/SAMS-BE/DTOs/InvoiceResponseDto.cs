using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class InvoiceResponseDto
    {
        public Guid InvoiceId { get; set; }
        public string InvoiceNo { get; set; } = default!;
        public Guid ApartmentId { get; set; }
        public DateOnly IssueDate { get; set; }
        public DateOnly DueDate { get; set; }
        public string Status { get; set; } = default!;
        public decimal SubtotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? TicketId { get; set; }  // TicketId đã được chuyển lên Invoice

        // ✅ NEW: Include invoice details for itemized view
        public List<InvoiceDetailResponseDto> Details { get; set; } = new();
    }
}
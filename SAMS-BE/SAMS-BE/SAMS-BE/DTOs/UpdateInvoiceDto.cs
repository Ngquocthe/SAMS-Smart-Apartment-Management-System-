using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class UpdateInvoiceDto
    {
        public Guid? ApartmentId { get; set; }
        public DateOnly? DueDate { get; set; }
        [StringLength(1000)]
        public string? Note { get; set; }
    }
}

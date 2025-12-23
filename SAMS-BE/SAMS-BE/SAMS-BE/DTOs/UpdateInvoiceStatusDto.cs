using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class UpdateInvoiceStatusDto
  {
        [Required]
        [RegularExpression("^(DRAFT|ISSUED|PAID|OVERDUE|CANCELLED)$", 
            ErrorMessage = "Status must be one of: DRAFT, ISSUED, PAID, OVERDUE, CANCELLED")]
        public string Status { get; set; } = null!;

        [StringLength(500)]
 public string? Note { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class UpdateVoucherDto
    {
        public DateOnly? Date { get; set; }
        public string? CompanyInfo { get; set; }
     [StringLength(1000)]
        public string? Description { get; set; }
    }

    public class UpdateVoucherStatusDto
    {
        [Required, StringLength(32)]
        public string Status { get; set; } = null!;
     public string? UpdatedBy { get; set; }
    }
}

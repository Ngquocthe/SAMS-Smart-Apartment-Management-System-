using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateServicePriceDto
    {
        [Required(ErrorMessage = "Unit price is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than zero.")]
        public decimal UnitPrice { get; set; }

        [Required(ErrorMessage = "Effective date is required.")]
        public DateOnly EffectiveDate { get; set; }
        public DateOnly? EndDate { get; set; }

        [MaxLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters.")]
        public string? Notes { get; set; }
    }
}

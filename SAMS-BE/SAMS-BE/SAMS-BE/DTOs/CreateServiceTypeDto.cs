using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    public class CreateServiceTypeDto
    {
        [Required, StringLength(50, MinimumLength = 2)]
        public string Code { get; set; } = default!;

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        [Required]
        public Guid CategoryId { get; set; }

        [Required, StringLength(32)]
        public string Unit { get; set; } = default!;

        [StringLength(255)]
        public string? Description { get; set; }

        public bool IsMandatory { get; set; } = false;
        public bool IsRecurring { get; set; } = false;
    }

}

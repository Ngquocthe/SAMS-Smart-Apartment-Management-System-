using System.ComponentModel.DataAnnotations;

public class UpdateServiceTypeDto
{
    [Required, StringLength(100)]
    public string Name { get; set; } = default!;

    [Required]
    public Guid CategoryId { get; set; }

    [StringLength(32)]
    public string Unit { get; set; } = default!;

    public bool IsMandatory { get; set; }
    public bool IsRecurring { get; set; }
    public bool? IsActive { get; set; }
}

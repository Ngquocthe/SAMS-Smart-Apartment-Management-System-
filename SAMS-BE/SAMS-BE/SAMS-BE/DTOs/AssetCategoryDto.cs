using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class AssetCategoryDto
{
    public Guid CategoryId { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(1000)]
    public string? Description { get; set; }

    public int? MaintenanceFrequency { get; set; }

    public int? DefaultReminderDays { get; set; }
}


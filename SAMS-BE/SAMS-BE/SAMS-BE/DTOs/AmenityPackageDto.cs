using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class AmenityPackageDto
{
    public Guid PackageId { get; set; }

    public Guid AmenityId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    public int MonthCount { get; set; }

    public int? DurationDays { get; set; }

    [StringLength(10)]
    public string? PeriodUnit { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Price must be non-negative")]
    public int Price { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(32)]
    public string Status { get; set; } = "ACTIVE";
}


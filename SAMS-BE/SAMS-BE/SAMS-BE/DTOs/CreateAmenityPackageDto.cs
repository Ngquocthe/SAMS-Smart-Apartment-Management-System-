using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class CreateAmenityPackageDto
{
    [Required(ErrorMessage = "Amenity ID is required")]
    public Guid AmenityId { get; set; }

    [Required(ErrorMessage = "Package name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = null!;

    public int MonthCount { get; set; }

    [Range(1, 365, ErrorMessage = "Duration days must be between 1 and 365")]
    public int? DurationDays { get; set; }

    [StringLength(10, ErrorMessage = "Period unit cannot exceed 10 characters")]
    [RegularExpression("^(Day|Month)$", ErrorMessage = "Period unit must be either 'Day' or 'Month'")]
    public string? PeriodUnit { get; set; }

    [Required(ErrorMessage = "Price is required")]
    [Range(10000, 10000000, ErrorMessage = "Price must be between 10,000 and 10,000,000")]
    public int Price { get; set; }

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }

    [StringLength(32, ErrorMessage = "Status cannot exceed 32 characters")]
    public string Status { get; set; } = "ACTIVE";
}


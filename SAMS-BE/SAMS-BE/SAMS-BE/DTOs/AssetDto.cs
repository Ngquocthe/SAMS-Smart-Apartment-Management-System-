using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class AssetDto
{
    public Guid AssetId { get; set; }

    public Guid CategoryId { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    public Guid? ApartmentId { get; set; }

    public Guid? BlockId { get; set; }

    [StringLength(255)]
    public string? Location { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public DateOnly? WarrantyExpire { get; set; }

    public int? MaintenanceFrequency { get; set; }

    [Required]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    public bool IsDelete { get; set; }

    // Navigation properties
    public AssetCategoryDto? Category { get; set; }
    public string? ApartmentNumber { get; set; }
}


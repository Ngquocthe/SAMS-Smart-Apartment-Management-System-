using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class AmenityDto
{
    public Guid AmenityId { get; set; }
    public Guid? AssetId { get; set; }

    [Required]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Required]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [StringLength(100)]
    public string? CategoryName { get; set; }

    [StringLength(255)]
    public string? Location { get; set; }

    public bool HasMonthlyPackage { get; set; }

    [Required]
    [StringLength(20)]
    public string FeeType { get; set; } = null!;

    [Required]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    public bool IsUnderMaintenance { get; set; }

    public DateTime? MaintenanceStart { get; set; }

    public DateTime? MaintenanceEnd { get; set; }

    public bool RequiresFaceVerification { get; set; }

    // Danh sách các gói có sẵn cho tiện ích này
    public List<AmenityPackageDto>? Packages { get; set; }
}

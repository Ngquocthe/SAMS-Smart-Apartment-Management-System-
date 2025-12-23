using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request;

/// <summary>
/// DTO cho lễ tân tạo booking cho cư dân
/// </summary>
public class ReceptionistCreateBookingRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Amenity ID is required")]
    public Guid AmenityId { get; set; }

    [Required(ErrorMessage = "Package ID is required")]
    public Guid PackageId { get; set; }

    /// <summary>
    /// ApartmentId của căn hộ đăng ký (optional)
    /// Nếu null: Backend tự động lấy primary apartment của user
    /// </summary>
    public Guid? ApartmentId { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}













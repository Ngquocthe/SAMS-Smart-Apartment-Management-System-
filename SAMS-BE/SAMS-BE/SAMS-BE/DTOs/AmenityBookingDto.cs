using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

/// <summary>
/// DTO cho response của AmenityBooking
/// </summary>
public class AmenityBookingDto
{
    public Guid BookingId { get; set; }

    public Guid AmenityId { get; set; }

    public string? AmenityName { get; set; }

    public Guid PackageId { get; set; }

    public string? PackageName { get; set; }

    public int? MonthCount { get; set; }

    public int? DurationDays { get; set; }

    public string? PeriodUnit { get; set; }

    public Guid ApartmentId { get; set; }

    public string? ApartmentCode { get; set; }

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? UserPhone { get; set; }

    public string? ResidentName { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    [StringLength(32)]
    public string Status { get; set; } = "Pending";

    public int TotalPrice { get; set; }

    public int Price { get; set; }

    [StringLength(32)]
    public string PaymentStatus { get; set; } = "Unpaid";

    [StringLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public AmenityDto? Amenity { get; set; }

    public AmenityPackageDto? Package { get; set; }

    public string? Location { get; set; }

    // Dùng cho FE để ẩn/hiện nút hủy ở màn cư dân
    public bool CanCancel { get; set; }
}

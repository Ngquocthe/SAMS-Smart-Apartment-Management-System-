namespace SAMS_BE.DTOs.Response;

/// <summary>
/// DTO cho cư dân đã đăng ký tiện ích
/// </summary>
public class RegisteredResidentDto
{
    public Guid UserId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? ApartmentCode { get; set; }
    public Guid? ApartmentId { get; set; }
    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
    public List<AmenityBookingDto> Bookings { get; set; } = new();
    public bool HasFaceRegistered { get; set; }
}













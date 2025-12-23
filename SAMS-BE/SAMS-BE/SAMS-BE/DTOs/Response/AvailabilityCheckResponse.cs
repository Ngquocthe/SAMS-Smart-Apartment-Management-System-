namespace SAMS_BE.DTOs.Response;

/// <summary>
/// Response cho endpoint check availability
/// </summary>
public class AvailabilityCheckResponse
{
    public bool IsAvailable { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ConflictingBookingInfo> ConflictingBookings { get; set; } = new();
}

/// <summary>
/// Thông tin booking bị trùng lịch (không còn sử dụng với hệ thống packages)
/// </summary>
public class ConflictingBookingInfo
{
    public Guid BookingId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? ApartmentCode { get; set; }
}


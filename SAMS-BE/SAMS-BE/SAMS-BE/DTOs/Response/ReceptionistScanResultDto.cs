using SAMS_BE.DTOs;

namespace SAMS_BE.DTOs.Response;

public class ReceptionistScanResultDto
{
    public bool Success { get; set; }
    public Guid? UserId { get; set; }
    public string? ResidentName { get; set; }
    public string? AvatarUrl { get; set; }
    public float Similarity { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool AlreadyCheckedInToday { get; set; }
    public AmenityBookingDto? Booking { get; set; }
    public AmenityCheckInDto? CheckIn { get; set; }
}



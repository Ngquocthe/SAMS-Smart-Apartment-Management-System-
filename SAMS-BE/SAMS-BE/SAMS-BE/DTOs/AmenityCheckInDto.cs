using System;

namespace SAMS_BE.DTOs;

public class AmenityCheckInDto
{
    public Guid CheckInId { get; set; }
    public Guid BookingId { get; set; }
    public Guid AmenityId { get; set; }
    public string AmenityName { get; set; } = string.Empty;
    public Guid ApartmentId { get; set; }
    public string ApartmentCode { get; set; } = string.Empty;
    public Guid CheckedInForUserId { get; set; }
    public string CheckedInForFullName { get; set; } = string.Empty;
    public string? CheckedInForPhone { get; set; }
    public Guid? CheckedInByUserId { get; set; }
    public string? CheckedInByFullName { get; set; }
    public double? Similarity { get; set; }
    public bool IsSuccess { get; set; }
    public string ResultStatus { get; set; } = string.Empty;
    public bool IsManualOverride { get; set; }
    public string? Message { get; set; }
    public DateTime CheckedInAt { get; set; }
    public string BookingStatus { get; set; } = string.Empty;
    public string? CapturedImageUrl { get; set; }
}



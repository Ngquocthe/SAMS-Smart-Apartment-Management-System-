using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request;

/// <summary>
/// Request DTO cho việc hủy booking
/// </summary>
public class CancelBookingRequest
{
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}


using System;
using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs.Request;

public class AmenityCheckInQueryDto
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    [Range(1, 200)]
    public int PageSize { get; set; } = 20;

    public Guid? AmenityId { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? CheckedInForUserId { get; set; }
    public Guid? CheckedInByUserId { get; set; }
    public bool? IsSuccess { get; set; }
    public string? ResultStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Search { get; set; }
}



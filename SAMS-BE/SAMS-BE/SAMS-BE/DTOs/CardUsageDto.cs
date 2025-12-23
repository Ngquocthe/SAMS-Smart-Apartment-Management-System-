using System;

namespace SAMS_BE.DTOs;

public class CardUsageDto
{
    public Guid UsageId { get; set; }
    public Guid CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string UsageType { get; set; } = null!; // LOGIN, LOGOUT, ACCESS_SYSTEM, etc.
    public string UsageName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime UsageTime { get; set; }
    public DateTime UsageTimeLocal { get; set; }
    public string? PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsDelete { get; set; }
}

public class CardUsageQueryDto
{
    public Guid? CardId { get; set; }
    public string? UsageType { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "UsageTime";
    public string? SortDirection { get; set; } = "desc";
}

public class CreateCardUsageDto
{
    public Guid CardId { get; set; }
    public string UsageType { get; set; } = null!;
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

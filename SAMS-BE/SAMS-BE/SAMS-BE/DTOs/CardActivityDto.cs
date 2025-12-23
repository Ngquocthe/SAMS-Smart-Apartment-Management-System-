using System;

namespace SAMS_BE.DTOs;

public class CardActivityDto
{
    public Guid ActivityId { get; set; }
    public Guid CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string ActivityType { get; set; } = null!; // CARD_CREATED, CARD_ACTIVATED, CARD_DEACTIVATED, CARD_RENEWED, etc.
    public string ActivityName { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime ActivityTime { get; set; }
    public DateTime ActivityTimeLocal { get; set; }
    public string? PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsDelete { get; set; }
}

public class CardActivityQueryDto
{
    public Guid? CardId { get; set; }
    public string? ActivityType { get; set; }
    public string? PerformedBy { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "ActivityTime";
    public string? SortDirection { get; set; } = "desc";
}

public class CreateCardActivityDto
{
    public Guid CardId { get; set; }
    public string ActivityType { get; set; } = null!;
    public string? Description { get; set; }
    public string? PerformedBy { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

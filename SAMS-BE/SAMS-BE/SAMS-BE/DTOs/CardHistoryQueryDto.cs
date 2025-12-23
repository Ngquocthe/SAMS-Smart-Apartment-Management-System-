using System;

namespace SAMS_BE.DTOs;

public class CardHistoryQueryDto
{
    public Guid? CardId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? ApartmentId { get; set; }
    public string? EventCode { get; set; }
    public string? FieldName { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; } = "EventTimeUtc";
    public string? SortDirection { get; set; } = "desc";
}

public class CardHistoryResponseDto
{
    public Guid CardHistoryId { get; set; }
    public Guid CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string? UserName { get; set; }
    public string? ApartmentNumber { get; set; }
    public Guid? CardTypeId { get; set; }
    public string? CardTypeName { get; set; }
    public string EventCode { get; set; } = null!;
    public string EventName { get; set; } = null!;
    public DateTime EventTimeUtc { get; set; }
    public DateTime EventTimeLocal { get; set; }
    public DateTime EventTimeVietnam { get; set; } // Giờ Việt Nam (UTC+7)
    public string? FieldName { get; set; }
    public string? FieldDisplayName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? CreatedBy { get; set; }
    public string? CreatedByName { get; set; } // Tên người thực hiện
    public DateTime CreatedAt { get; set; }
    public DateTime CreatedAtVietnam { get; set; } // Giờ Việt Nam (UTC+7)
    public bool IsDelete { get; set; }
}

public class CardAccessSummaryDto
{
    public Guid CardId { get; set; }
    public string CardNumber { get; set; } = null!;
    public string? UserName { get; set; }
    public string? ApartmentNumber { get; set; }
    public int TotalAccess { get; set; }
    public int SuccessfulAccess { get; set; }
    public int FailedAccess { get; set; }
    public DateTime? LastAccessTime { get; set; }
    public List<string> AccessedAreas { get; set; } = new List<string>();
    public List<string> EventTypes { get; set; } = new List<string>();
}

using System;

namespace SAMS_BE.DTOs;

public class CardHistoryDto
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
    public DateTime CreatedAt { get; set; }
    public DateTime CreatedAtVietnam { get; set; } // Giờ Việt Nam (UTC+7)
    public bool IsDelete { get; set; }
}

public class CreateCardHistoryDto
{
    public Guid CardId { get; set; }
    public Guid? CardTypeId { get; set; }
    public string EventCode { get; set; } = null!;
    public DateTime? EventTimeUtc { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Description { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string CreatedBy { get; set; } = "system";
}

using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class AssetMaintenanceScheduleDto
{
    public Guid ScheduleId { get; set; }

    public Guid AssetId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Giờ bắt đầu bảo trì (khung giờ)
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Giờ kết thúc bảo trì (khung giờ)
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    public int ReminderDays { get; set; } = 3;

    [StringLength(500)]
    public string? Description { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    [Required]
    [StringLength(32)]
    public string Status { get; set; } = "SCHEDULED";

    [StringLength(32)]
    public string? RecurrenceType { get; set; }

    public int? RecurrenceInterval { get; set; }

    public DateOnly? ScheduledStartDate { get; set; }
    public DateOnly? ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? CompletionNotes { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Navigation properties
    public AssetDto? Asset { get; set; }
    public string? CompletedByUserName { get; set; }
    public string? CreatedByUserName { get; set; }
    
    /// <summary>
    /// Danh sách tickets liên quan đến lịch bảo trì này
    /// </summary>
    public List<TicketDto>? Tickets { get; set; }
    
    /// <summary>
    /// Danh sách lịch sử bảo trì liên quan đến lịch bảo trì này
    /// </summary>
    public List<AssetMaintenanceHistoryDto>? MaintenanceHistories { get; set; }
}

public class CreateAssetMaintenanceScheduleDto
{
    [Required]
    public Guid AssetId { get; set; }

    [Required]
    public DateOnly StartDate { get; set; }

    [Required]
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Giờ bắt đầu bảo trì (khung giờ)
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Giờ kết thúc bảo trì (khung giờ)
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    public int ReminderDays { get; set; } = 3;

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
    public string? Description { get; set; }

    [StringLength(32)]
    public string Status { get; set; } = "SCHEDULED";

    [StringLength(32)]
    public string? RecurrenceType { get; set; }

    public int? RecurrenceInterval { get; set; }

    /// <summary>
    /// Ngày bắt đầu dự kiến (scheduled)
    /// </summary>
    public DateOnly? ScheduledStartDate { get; set; }

    /// <summary>
    /// Ngày kết thúc dự kiến (scheduled)
    /// </summary>
    public DateOnly? ScheduledEndDate { get; set; }
}

public class UpdateAssetMaintenanceScheduleDto
{
    public Guid? AssetId { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public int? ReminderDays { get; set; }

    [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự")]
    public string? Description { get; set; }

    [StringLength(32)]
    public string? Status { get; set; }

    [StringLength(32)]
    public string? RecurrenceType { get; set; }

    public int? RecurrenceInterval { get; set; }

    /// <summary>
    /// Giờ bắt đầu bảo trì (khung giờ)
    /// </summary>
    public TimeOnly? StartTime { get; set; }

    /// <summary>
    /// Giờ kết thúc bảo trì (khung giờ)
    /// </summary>
    public TimeOnly? EndTime { get; set; }

    /// <summary>
    /// Ghi chú khi hoàn thành
    /// </summary>
    [StringLength(1000, ErrorMessage = "Ghi chú hoàn thành không được vượt quá 1000 ký tự")]
    public string? CompletionNotes { get; set; }

    /// <summary>
    /// Ngày giờ thực tế hoàn thành
    /// </summary>
    public DateTime? ActualEndDate { get; set; }
}

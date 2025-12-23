using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs;

public class AssetMaintenanceHistoryDto
{
    public Guid HistoryId { get; set; }
    public Guid AssetId { get; set; }
    public Guid? ScheduleId { get; set; }
    public DateTime ActionDate { get; set; }
    [Required]
    [StringLength(255)]
    public string Action { get; set; } = null!;
    public decimal? CostAmount { get; set; }
    [StringLength(1000)]
    public string? Notes { get; set; }
    public DateOnly? NextDueDate { get; set; }
    
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public DateOnly? ScheduledStartDate { get; set; }
    public DateOnly? ScheduledEndDate { get; set; }
    public string? CompletionStatus { get; set; }
    public int? DaysDifference { get; set; }
    public Guid? PerformedBy { get; set; }
    
    // Người thực hiện (lấy từ Schedule nếu có)
    public string? CreatedByUserName { get; set; }
    public string? PerformedByUserName { get; set; }
    
    // Navigation properties
    public AssetDto? Asset { get; set; }
    public AssetMaintenanceScheduleDto? Schedule { get; set; }
}

public class CreateAssetMaintenanceHistoryDto
{
    [Required]
    public Guid AssetId { get; set; }
    
    public Guid? ScheduleId { get; set; }
    
    public DateTime? ActionDate { get; set; }
    
    [Required(ErrorMessage = "Action is required")]
    [StringLength(255, ErrorMessage = "Action cannot exceed 255 characters")]
    public string Action { get; set; } = null!;
    
    public decimal? CostAmount { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
    
    public DateOnly? NextDueDate { get; set; }
    
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public DateOnly? ScheduledStartDate { get; set; }
    public DateOnly? ScheduledEndDate { get; set; }
}

public class UpdateAssetMaintenanceHistoryDto
{
    public DateTime? ActionDate { get; set; }
    
    [StringLength(255)]
    public string? Action { get; set; }
    
    public decimal? CostAmount { get; set; }
    
    [StringLength(1000)]
    public string? Notes { get; set; }
    
    public DateOnly? NextDueDate { get; set; }
}


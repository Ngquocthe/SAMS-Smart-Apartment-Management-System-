using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("asset_maintenance_history", Schema = "building")]
public partial class AssetMaintenanceHistory
{
    [Key]
    [Column("history_id")]
    public Guid HistoryId { get; set; }

    [Column("asset_id")]
    public Guid AssetId { get; set; }

    [Column("schedule_id")]
    public Guid? ScheduleId { get; set; }

    [Column("action_date")]
    [Precision(3)]
    public DateTime ActionDate { get; set; }

    [Column("action")]
    [StringLength(255)]
    public string Action { get; set; } = null!;

    [Column("cost_amount", TypeName = "decimal(18, 2)")]
    public decimal? CostAmount { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("next_due_date")]
    public DateOnly? NextDueDate { get; set; }

    [Column("actual_start_date")]
    [Precision(3)]
    public DateTime? ActualStartDate { get; set; }

    [Column("actual_end_date")]
    [Precision(3)]
    public DateTime? ActualEndDate { get; set; }

    [Column("scheduled_start_date")]
    public DateOnly? ScheduledStartDate { get; set; }

    [Column("scheduled_end_date")]
    public DateOnly? ScheduledEndDate { get; set; }

    [Column("completion_status")]
    [StringLength(32)]
    public string? CompletionStatus { get; set; }

    [Column("days_difference")]
    public int? DaysDifference { get; set; }

    [Column("performed_by")]
    public Guid? PerformedBy { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("AssetMaintenanceHistories")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("PerformedBy")]
    [InverseProperty("PerformedAssetMaintenanceHistories")]
    public virtual User? PerformedByUser { get; set; }

    [ForeignKey("ScheduleId")]
    [InverseProperty("AssetMaintenanceHistories")]
    public virtual AssetMaintenanceSchedule? Schedule { get; set; }

    [InverseProperty("History")]
    public virtual ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}

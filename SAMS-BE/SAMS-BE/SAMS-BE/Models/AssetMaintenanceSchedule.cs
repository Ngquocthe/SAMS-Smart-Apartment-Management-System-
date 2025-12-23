using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("asset_maintenance_schedule", Schema = "building")]
public partial class AssetMaintenanceSchedule
{
    [Key]
    [Column("schedule_id")]
    public Guid ScheduleId { get; set; }

    [Column("asset_id")]
    public Guid AssetId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("start_time")]
    public TimeOnly? StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly? EndTime { get; set; }

    [Column("reminder_days")]
    public int ReminderDays { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("recurrence_type")]
    [StringLength(32)]
    public string? RecurrenceType { get; set; }

    [Column("recurrence_interval")]
    public int? RecurrenceInterval { get; set; }

    [Column("scheduled_start_date")]
    public DateOnly? ScheduledStartDate { get; set; }

    [Column("scheduled_end_date")]
    public DateOnly? ScheduledEndDate { get; set; }

    [Column("actual_start_date")]
    [Precision(3)]
    public DateTime? ActualStartDate { get; set; }

    [Column("actual_end_date")]
    [Precision(3)]
    public DateTime? ActualEndDate { get; set; }

    [Column("completion_notes")]
    [StringLength(1000)]
    public string? CompletionNotes { get; set; }

    [Column("completed_by")]
    public Guid? CompletedBy { get; set; }

    [Column("completed_at")]
    [Precision(3)]
    public DateTime? CompletedAt { get; set; }

    [ForeignKey("AssetId")]
    [InverseProperty("AssetMaintenanceSchedules")]
    public virtual Asset Asset { get; set; } = null!;

    [ForeignKey("CompletedBy")]
    [InverseProperty("CompletedAssetMaintenanceSchedules")]
    public virtual User? CompletedByUser { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("AssetMaintenanceSchedules")]
    public virtual User? CreatedByUser { get; set; }

    [InverseProperty("Schedule")]
    public virtual ICollection<AssetMaintenanceHistory> AssetMaintenanceHistories { get; set; } = new List<AssetMaintenanceHistory>();

    [InverseProperty("Schedule")]
    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}


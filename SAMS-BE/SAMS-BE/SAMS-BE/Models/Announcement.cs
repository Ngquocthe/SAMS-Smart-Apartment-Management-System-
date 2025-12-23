using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("announcements", Schema = "building")]
public partial class Announcement
{
    [Key]
    [Column("announcement_id")]
    public Guid AnnouncementId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("visible_from")]
    [Precision(3)]
    public DateTime VisibleFrom { get; set; }

    [Column("visible_to")]
    [Precision(3)]
    public DateTime? VisibleTo { get; set; }

    [Column("visibility_scope")]
    [StringLength(255)]
    public string? VisibilityScope { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("is_pinned")]
    public bool IsPinned { get; set; }

    [Column("type")]
    [StringLength(50)]
    public string? Type { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    [StringLength(190)]
    public string? UpdatedBy { get; set; }

    [Column("schedule_id")]
    public Guid? ScheduleId { get; set; }

    [Column("booking_id")]
    public Guid? BookingId { get; set; }

    [ForeignKey("ScheduleId")]
    [InverseProperty("Announcements")]
    public virtual AssetMaintenanceSchedule? Schedule { get; set; }

    [ForeignKey("BookingId")]
    [InverseProperty("Announcements")]
    public virtual AmenityBooking? Booking { get; set; }

    [InverseProperty("Announcement")]
    public virtual AnnouncementRead? AnnouncementRead { get; set; }
}

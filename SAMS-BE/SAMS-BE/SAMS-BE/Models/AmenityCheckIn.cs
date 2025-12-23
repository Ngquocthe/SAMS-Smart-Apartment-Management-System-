using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("amenity_check_ins", Schema = "building")]
public partial class AmenityCheckIn
{
    [Key]
    [Column("check_in_id")]
    public Guid CheckInId { get; set; }

    [Column("booking_id")]
    public Guid BookingId { get; set; }

    [Column("checked_in_for_user_id")]
    public Guid CheckedInForUserId { get; set; }

    [Column("checked_in_by_user_id")]
    public Guid? CheckedInByUserId { get; set; }

    [Column("similarity")]
    public double? Similarity { get; set; }

    [Column("is_success")]
    public bool IsSuccess { get; set; } = true;

    [Column("result_status")]
    [StringLength(32)]
    public string ResultStatus { get; set; } = "Success";

    [Column("message")]
    [StringLength(500)]
    public string? Message { get; set; }

    [Column("captured_image_url")]
    [StringLength(500)]
    public string? CapturedImageUrl { get; set; }

    [Column("is_manual_override")]
    public bool IsManualOverride { get; set; }

    [Column("checked_in_at")]
    [Precision(3)]
    public DateTime CheckedInAt { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [ForeignKey(nameof(BookingId))]
    public virtual AmenityBooking Booking { get; set; } = null!;

    [ForeignKey(nameof(CheckedInForUserId))]
    public virtual User CheckedInForUser { get; set; } = null!;

    [ForeignKey(nameof(CheckedInByUserId))]
    public virtual User? CheckedInByUser { get; set; }
}



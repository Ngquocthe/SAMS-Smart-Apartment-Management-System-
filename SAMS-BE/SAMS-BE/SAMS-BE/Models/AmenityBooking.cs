using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("amenity_bookings", Schema = "building")]
public partial class AmenityBooking
{
    [Key]
    [Column("booking_id")]
    public Guid BookingId { get; set; }

    [Column("amenity_id")]
    public Guid AmenityId { get; set; }

    [Column("package_id")]
    public Guid PackageId { get; set; }

    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly EndDate { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("total_price")]
    public int TotalPrice { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = "Pending";

    [Column("payment_status")]
    [StringLength(32)]
    public string PaymentStatus { get; set; } = "Unpaid";

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

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

    [Column("is_delete")]
    public bool IsDelete { get; set; } = false;

    [ForeignKey("AmenityId")]
    [InverseProperty("AmenityBookings")]
    public virtual Amenity Amenity { get; set; } = null!;

    [ForeignKey("PackageId")]
    [InverseProperty("AmenityBookings")]
    public virtual AmenityPackage Package { get; set; } = null!;

    [ForeignKey("ApartmentId")]
    [InverseProperty("AmenityBookings")]
    public virtual Apartment Apartment { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("AmenityBookings")]
    public virtual User? User { get; set; }

    [InverseProperty("Booking")]
    public virtual ICollection<AmenityCheckIn> AmenityCheckIns { get; set; } = new List<AmenityCheckIn>();

    [InverseProperty("Booking")]
    public virtual ICollection<Announcement> Announcements { get; set; } = new List<Announcement>();
}

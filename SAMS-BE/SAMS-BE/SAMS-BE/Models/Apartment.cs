using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("apartments", Schema = "building")]
public partial class Apartment
{
    [Key]
    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("floor_id")]
    public Guid FloorId { get; set; }

    [Column("number")]
    [StringLength(64)]
    public string Number { get; set; } = null!;

    [Column("area_m2", TypeName = "decimal(10, 2)")]
    public decimal? AreaM2 { get; set; }

    [Column("bedrooms")]
    public int? Bedrooms { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("image")]
    [StringLength(250)]
    public string? Image { get; set; }

    [Column("type")]
    [StringLength(100)]
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

    [InverseProperty("IssuedToApartment")]
    public virtual ICollection<AccessCard> AccessCards { get; set; } = new List<AccessCard>();

    [InverseProperty("Apartment")]
    public virtual ICollection<AmenityBooking> AmenityBookings { get; set; } = new List<AmenityBooking>();

    [InverseProperty("Apartment")]
    public virtual ICollection<ApartmentService> ApartmentServices { get; set; } = new List<ApartmentService>();

    [InverseProperty("Apartment")]
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    [InverseProperty("Apartment")]
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();

    [ForeignKey("FloorId")]
    [InverseProperty("Apartments")]
    public virtual Floor Floor { get; set; } = null!;

    [InverseProperty("Apartment")]
    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    [InverseProperty("Apartment")]
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();

    [InverseProperty("Apartment")]
    public virtual ICollection<MaintenanceApartmentHistory> MaintenanceApartmentHistories { get; set; } = new List<MaintenanceApartmentHistory>();

    [InverseProperty("Apartment")]
    public virtual ICollection<Meter> Meters { get; set; } = new List<Meter>();

    [InverseProperty("Apartment")]
    public virtual ICollection<ResidentApartment> ResidentApartments { get; set; } = new List<ResidentApartment>();

    [InverseProperty("Apartment")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    [InverseProperty("Apartment")]
    public virtual ICollection<VoucherItem> VoucherItems { get; set; } = new List<VoucherItem>();

    [InverseProperty("Apartment")]
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

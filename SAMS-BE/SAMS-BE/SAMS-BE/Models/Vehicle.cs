using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("vehicles", Schema = "building")]
[Index("ResidentId", Name = "IX_vehicles_resident")]
[Index("LicensePlate", Name = "UQ_vehicles_plate", IsUnique = true)]
public partial class Vehicle
{
    [Key]
    [Column("vehicle_id")]
    public Guid VehicleId { get; set; }

    [Column("resident_id")]
    public Guid? ResidentId { get; set; }

    [Column("apartment_id")]
    public Guid? ApartmentId { get; set; }

    [Column("vehicle_type_id")]
    public Guid VehicleTypeId { get; set; }

    [Column("license_plate")]
    [StringLength(64)]
    public string LicensePlate { get; set; } = null!;

    [Column("color")]
    [StringLength(64)]
    public string? Color { get; set; }

    [Column("brand_model")]
    [StringLength(128)]
    public string? BrandModel { get; set; }

    [Column("parking_card_id")]
    public Guid? ParkingCardId { get; set; }

    [Column("registered_at")]
    [Precision(3)]
    public DateTime RegisteredAt { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("meta")]
    public string? Meta { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("Vehicles")]
    public virtual Apartment? Apartment { get; set; }

    [ForeignKey("ParkingCardId")]
    [InverseProperty("Vehicles")]
    public virtual AccessCard? ParkingCard { get; set; }

    [InverseProperty("Vehicle")]
    public virtual ICollection<ParkingEntry> ParkingEntries { get; set; } = new List<ParkingEntry>();

    [ForeignKey("ResidentId")]
    [InverseProperty("Vehicles")]
    public virtual ResidentProfile? Resident { get; set; }

    [ForeignKey("VehicleTypeId")]
    [InverseProperty("Vehicles")]
    public virtual VehicleType VehicleType { get; set; } = null!;
}

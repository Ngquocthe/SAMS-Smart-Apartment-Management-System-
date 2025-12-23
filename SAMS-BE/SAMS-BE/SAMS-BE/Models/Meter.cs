using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("meters", Schema = "building")]
[Index("ApartmentId", "ServiceId", "SerialNo", Name = "UQ_meters", IsUnique = true)]
public partial class Meter
{
    [Key]
    [Column("meter_id")]
    public Guid MeterId { get; set; }

    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("service_id")]
    public Guid ServiceId { get; set; }

    [Column("serial_no")]
    [StringLength(128)]
    public string SerialNo { get; set; } = null!;

    [Column("installed_at")]
    [Precision(3)]
    public DateTime InstalledAt { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("meta")]
    public string? Meta { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("Meters")]
    public virtual Apartment Apartment { get; set; } = null!;

    [InverseProperty("Meter")]
    public virtual ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();

    [ForeignKey("ServiceId")]
    [InverseProperty("Meters")]
    public virtual ServiceType Service { get; set; } = null!;
}

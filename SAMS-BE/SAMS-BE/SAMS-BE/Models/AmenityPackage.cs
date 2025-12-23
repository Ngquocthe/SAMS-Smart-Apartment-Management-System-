using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("amenity_packages", Schema = "building")]
public partial class AmenityPackage
{
    [Key]
    [Column("package_id")]
    public Guid PackageId { get; set; }

    [Column("amenity_id")]
    public Guid AmenityId { get; set; }

    [Column("name")]
    [StringLength(100)]
    public string Name { get; set; } = null!;

    [Column("month_count")]
    public int MonthCount { get; set; }

    [Column("duration_days")]
    public int? DurationDays { get; set; }

    [Column("period_unit")]
    [StringLength(10)]
    public string? PeriodUnit { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = "ACTIVE";

    [ForeignKey("AmenityId")]
    [InverseProperty("AmenityPackages")]
    public virtual Amenity Amenity { get; set; } = null!;

    [InverseProperty("Package")]
    public virtual ICollection<AmenityBooking> AmenityBookings { get; set; } = new List<AmenityBooking>();
}


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("amenities", Schema = "building")]
[Index("Code", Name = "UQ_amenities_code", IsUnique = true)]
public partial class Amenity
{
    [Key]
    [Column("amenity_id")]
    public Guid AmenityId { get; set; }

    [Column("asset_id")]
    public Guid? AssetId { get; set; }

    [Column("code")]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("category_name")]
    [StringLength(100)]
    public string? CategoryName { get; set; }

    [Column("location")]
    [StringLength(255)]
    public string? Location { get; set; }

    [Column("has_monthly_package")]
    public bool HasMonthlyPackage { get; set; }

    [Column("fee_type")]
    [StringLength(20)]
    public string FeeType { get; set; } = null!;

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("is_delete")]
    public bool IsDelete { get; set; } = false;

    [ForeignKey("AssetId")]
    [InverseProperty("Amenities")]
    public virtual Asset? Asset { get; set; }

    [Column("requires_face_verification")]
    public bool RequiresFaceVerification { get; set; } = false;

    [InverseProperty("Amenity")]
    public virtual ICollection<AmenityBooking> AmenityBookings { get; set; } = new List<AmenityBooking>();

    [InverseProperty("Amenity")]
    public virtual ICollection<AmenityPackage> AmenityPackages { get; set; } = new List<AmenityPackage>();
}

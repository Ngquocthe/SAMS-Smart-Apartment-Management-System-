using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("assets", Schema = "building")]
[Index("Code", Name = "UQ_assets_code", IsUnique = true)]
public partial class Asset
{
    [Key]
    [Column("asset_id")]
    public Guid AssetId { get; set; }

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("code")]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("apartment_id")]
    public Guid? ApartmentId { get; set; }

    [Column("block_id")]
    public Guid? BlockId { get; set; }

    [Column("location")]
    [StringLength(255)]
    public string? Location { get; set; }

    [Column("purchase_date")]
    public DateOnly? PurchaseDate { get; set; }

    [Column("warranty_expire")]
    public DateOnly? WarrantyExpire { get; set; }

    [Column("maintenance_frequency")]
    public int? MaintenanceFrequency { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("is_delete")]
    public bool IsDelete { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("Assets")]
    public virtual Apartment? Apartment { get; set; }

    [InverseProperty("Asset")]
    public virtual ICollection<Amenity> Amenities { get; set; } = new List<Amenity>();

    [InverseProperty("Asset")]
    public virtual ICollection<AssetMaintenanceHistory> AssetMaintenanceHistories { get; set; } = new List<AssetMaintenanceHistory>();

    [InverseProperty("Asset")]
    public virtual ICollection<AssetMaintenanceSchedule> AssetMaintenanceSchedules { get; set; } = new List<AssetMaintenanceSchedule>();

    [ForeignKey("CategoryId")]
    [InverseProperty("Assets")]
    public virtual AssetCategory Category { get; set; } = null!;
}

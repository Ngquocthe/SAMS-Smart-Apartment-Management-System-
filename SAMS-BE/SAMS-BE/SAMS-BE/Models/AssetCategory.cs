using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("asset_categories", Schema = "building")]
[Index("Code", Name = "UQ_asset_categories_code", IsUnique = true)]
public partial class AssetCategory
{
    [Key]
    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("code")]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Column("maintenance_frequency")]
    public int? MaintenanceFrequency { get; set; }

    [Column("default_reminder_days")]
    public int? DefaultReminderDays { get; set; }

    [InverseProperty("Category")]
    public virtual ICollection<Asset> Assets { get; set; } = new List<Asset>();
}

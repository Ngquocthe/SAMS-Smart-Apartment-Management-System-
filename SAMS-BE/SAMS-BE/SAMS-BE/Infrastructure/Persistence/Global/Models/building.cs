using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Infrastructure.Persistence.Global.Models;

[Table("building")]
[Index("code", Name = "UQ_building_code", IsUnique = true)]
[Index("schema_name", Name = "UQ_building_schema", IsUnique = true)]
public partial class building
{
    [Key]
    public Guid id { get; set; }

    [StringLength(30)]
    public string code { get; set; } = null!;

    [StringLength(128)]
    public string schema_name { get; set; } = null!;

    [StringLength(150)]
    public string building_name { get; set; } = null!;

    public byte status { get; set; }

    [Precision(3)]
    public DateTime create_at { get; set; }

    [Precision(3)]
    public DateTime? update_at { get; set; }

    [StringLength(500)]
    public string? image_url { get; set; }

    public string? description { get; set; }

    [Precision(12, 2)]
    public decimal? total_area_m2 { get; set; }

    [Column(TypeName = "date")]
    public DateTime? opening_date { get; set; }

    [Precision(10, 7)]
    public decimal? latitude { get; set; }

    [Precision(10, 7)]
    public decimal? longitude { get; set; }

    public bool is_deleted { get; set; }

    [Precision(3)]
    public DateTime? deleted_at { get; set; }

    public Guid? created_by { get; set; }

    public Guid? updated_by { get; set; }

    [InverseProperty("building")]
    public virtual ICollection<audit_log_global> audit_log_globals { get; set; } = new List<audit_log_global>();

    [InverseProperty("building")]
    public virtual ICollection<user_building> user_buildings { get; set; } = new List<user_building>();
}

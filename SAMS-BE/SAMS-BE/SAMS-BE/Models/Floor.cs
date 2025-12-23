using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SAMS_BE.Enums;

namespace SAMS_BE.Models;

[Table("floors", Schema = "building")]
public partial class Floor
{
    [Key]
    [Column("floor_id")]
    public Guid FloorId { get; set; }

    [Column("floor_number")]
    public int FloorNumber { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string? Name { get; set; }

    [Column("floor_type")]
    [StringLength(50)]
    public string? FloorType { get; set; }

    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Floor")]
    public virtual ICollection<Apartment> Apartments { get; set; } = new List<Apartment>();
}

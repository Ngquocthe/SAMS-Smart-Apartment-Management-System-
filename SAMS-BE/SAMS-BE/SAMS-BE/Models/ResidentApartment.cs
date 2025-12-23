using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("resident_apartments", Schema = "building")]
[Index("ApartmentId", Name = "IX_resident_apartments_apartment")]
[Index("ResidentId", "ApartmentId", "RelationType", "StartDate", Name = "UQ_ra_unique", IsUnique = true)]
public partial class ResidentApartment
{
    [Key]
    [Column("resident_apartment_id")]
    public Guid ResidentApartmentId { get; set; }

    [Column("resident_id")]
    public Guid ResidentId { get; set; }

    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("relation_type")]
    [StringLength(32)]
    public string RelationType { get; set; } = null!;

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("is_primary")]
    public bool IsPrimary { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("ResidentApartments")]
    public virtual Apartment Apartment { get; set; } = null!;

    [ForeignKey("ResidentId")]
    [InverseProperty("ResidentApartments")]
    public virtual ResidentProfile Resident { get; set; } = null!;
}

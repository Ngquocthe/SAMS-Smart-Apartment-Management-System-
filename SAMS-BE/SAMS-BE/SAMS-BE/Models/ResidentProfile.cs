using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("resident_profiles")]
[Index("UserId", Name = "UQ_resident_profiles_user", IsUnique = true)]
public partial class ResidentProfile
{
    [Key]
    [Column("resident_id")]
    public Guid ResidentId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("phone")]
    [StringLength(50)]
    public string? Phone { get; set; }

    [Column("email")]
    [StringLength(190)]
    public string? Email { get; set; }

    [Column("id_number")]
    [StringLength(64)]
    public string? IdNumber { get; set; }

    [Column("dob")]
    public DateOnly? Dob { get; set; }

    [Column("gender")]
    [StringLength(16)]
    public string? Gender { get; set; }

    [Column("address")]
    [StringLength(500)]
    public string? Address { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("is_verified")]
    public bool IsVerified { get; set; }

    [Column("verified_at")]
    [Precision(3)]
    public DateTime? VerifiedAt { get; set; }

    [Column("nationality")]
    [StringLength(64)]
    public string? Nationality { get; set; }

    [Column("internal_note")]
    [StringLength(1000)]
    public string? InternalNote { get; set; }

    [Column("meta")]
    public string? Meta { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Resident")]
    public virtual ICollection<ResidentApartment> ResidentApartments { get; set; } = new List<ResidentApartment>();

    [ForeignKey("UserId")]
    [InverseProperty("ResidentProfile")]
    public virtual User? User { get; set; }

    [InverseProperty("Resident")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

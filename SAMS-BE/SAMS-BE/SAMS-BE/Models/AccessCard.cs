using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("access_cards", Schema = "building")]
[Index("CardNumber", Name = "UQ_access_cards_number", IsUnique = true)]
public partial class AccessCard
{
    [Key]
    [Column("card_id")]
    public Guid CardId { get; set; }

    [Column("card_number")]
    [StringLength(128)]
    public string CardNumber { get; set; } = null!;

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = "ACTIVE";

    [Column("issued_to_user_id")]
    public Guid? IssuedToUserId { get; set; }

    [Column("issued_to_apartment_id")]
    public Guid? IssuedToApartmentId { get; set; }

    [Column("issued_date")]
    [Precision(3)]
    public DateTime IssuedDate { get; set; } = DateTime.UtcNow;

    [Column("expired_date")]
    [Precision(3)]
    public DateTime? ExpiredDate { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [Column("updated_by")]
    [StringLength(190)]
    public string? UpdatedBy { get; set; }

    [Column("is_delete")]
    public bool IsDelete { get; set; } = false;


    [ForeignKey("IssuedToApartmentId")]
    [InverseProperty("AccessCards")]
    public virtual Apartment? IssuedToApartment { get; set; }

    [ForeignKey("IssuedToUserId")]
    [InverseProperty("AccessCards")]
    public virtual User? IssuedToUser { get; set; }

    [InverseProperty("Card")]
    public virtual ICollection<CardHistory> CardHistories { get; set; } = new List<CardHistory>();

    [InverseProperty("Card")]
    public virtual ICollection<ParkingEntry> ParkingEntries { get; set; } = new List<ParkingEntry>();

    [InverseProperty("ParkingCard")]
    public virtual ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

    [InverseProperty("Card")]
    public virtual ICollection<AccessCardCapability> AccessCardCapabilities { get; set; } = new List<AccessCardCapability>();
}

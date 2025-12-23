using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("users", Schema = "building")]
[Index("Email", Name = "UQ_users_email", IsUnique = true)]
[Index("Username", Name = "UQ_users_username", IsUnique = true)]
public partial class User
{
    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("username")]
    [StringLength(50)]
    public string Username { get; set; } = null!;

    [Column("email")]
    [StringLength(50)]
    public string Email { get; set; } = null!;

    [Column("phone")]
    [StringLength(20)]
    public string Phone { get; set; } = null!;

    [Column("first_name")]
    [StringLength(100)]
    public string FirstName { get; set; } = null!;

    [Column("last_name")]
    [StringLength(100)]
    public string LastName { get; set; } = null!;

    [Column("dob")]
    public DateOnly? Dob { get; set; }

    [Column("address")]
    [StringLength(150)]
    public string? Address { get; set; }

    [Column("avatar_url")]
    [StringLength(300)]
    public string? AvatarUrl { get; set; }

    [Column("checkin_photo_url")]
    [StringLength(300)]
    public string? CheckinPhotoUrl { get; set; }

    [Column("face_embedding")]
    public byte[]? FaceEmbedding { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("IssuedToUser")]
    public virtual ICollection<AccessCard> AccessCards { get; set; } = new List<AccessCard>();

    [InverseProperty("User")]
    public virtual ICollection<AmenityBooking> AmenityBookings { get; set; } = new List<AmenityBooking>();

    [InverseProperty("User")]
    public virtual ICollection<AnnouncementRead> AnnouncementReads { get; set; } = new List<AnnouncementRead>();

    [InverseProperty("CreatorUser")]
    public virtual ICollection<MaintenanceApartmentHistory> MaintenanceApartmentHistoryCreatorUsers { get; set; } = new List<MaintenanceApartmentHistory>();

    [InverseProperty("HandlerUser")]
    public virtual ICollection<MaintenanceApartmentHistory> MaintenanceApartmentHistoryHandlerUsers { get; set; } = new List<MaintenanceApartmentHistory>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<Receipt> Receipts { get; set; } = new List<Receipt>();

    [InverseProperty("User")]
    public virtual ResidentProfile? ResidentProfile { get; set; }

    [InverseProperty("CheckedInByUser")]
    public virtual ICollection<AmenityCheckIn> AmenityCheckInsHandled { get; set; } = new List<AmenityCheckIn>();

    [InverseProperty("CheckedInForUser")]
    public virtual ICollection<AmenityCheckIn> AmenityCheckInsAsTarget { get; set; } = new List<AmenityCheckIn>();

    [InverseProperty("User")]
    public virtual ICollection<StaffProfile> StaffProfiles { get; set; } = new List<StaffProfile>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<Ticket> TicketCreatedByUsers { get; set; } = new List<Ticket>();

    [InverseProperty("CreatedByUser")]
    public virtual ICollection<AssetMaintenanceSchedule> AssetMaintenanceSchedules { get; set; } = new List<AssetMaintenanceSchedule>();

    [InverseProperty("CompletedByUser")]
    public virtual ICollection<AssetMaintenanceSchedule> CompletedAssetMaintenanceSchedules { get; set; } = new List<AssetMaintenanceSchedule>();

    [InverseProperty("PerformedByUser")]
    public virtual ICollection<AssetMaintenanceHistory> PerformedAssetMaintenanceHistories { get; set; } = new List<AssetMaintenanceHistory>();
}

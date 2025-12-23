using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("announcement_reads", Schema = "building")]
[Index("AnnouncementId", Name = "UQ_announcement_reads", IsUnique = true)]
public partial class AnnouncementRead
{
    [Key]
    [Column("announcement_read_id")]
    public Guid AnnouncementReadId { get; set; }

    [Column("announcement_id")]
    public Guid AnnouncementId { get; set; }

    [Column("user_id")]
    public Guid? UserId { get; set; }

    [Column("read_at")]
    [Precision(3)]
    public DateTime ReadAt { get; set; }

    [ForeignKey("AnnouncementId")]
    [InverseProperty("AnnouncementRead")]
    public virtual Announcement Announcement { get; set; } = null!;

    [ForeignKey("UserId")]
    [InverseProperty("AnnouncementReads")]
    public virtual User? User { get; set; }
}

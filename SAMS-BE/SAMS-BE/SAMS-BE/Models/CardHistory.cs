using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("card_history", Schema = "building")]
public partial class CardHistory
{
    [Key]
    [Column("card_history_id")]
    public Guid CardHistoryId { get; set; }

    [Column("card_id")]
    public Guid CardId { get; set; }

    [Column("card_type_id")]
    public Guid? CardTypeId { get; set; }

    [Column("event_code")]
    [StringLength(64)]
    public string EventCode { get; set; } = null!; // OWNER_CHANGE, APARTMENT_CHANGE, CAPABILITY_CHANGE, etc.

    [Column("event_time_utc")]
    [Precision(3)]
    public DateTime EventTimeUtc { get; set; } = DateTime.UtcNow.AddHours(7); // Giờ Việt Nam (UTC+7)

    [Column("field_name")]
    [StringLength(128)]
    public string? FieldName { get; set; } // IssuedToUserId, IssuedToApartmentId, Capabilities, etc.

    [Column("old_value")]
    [StringLength(500)]
    public string? OldValue { get; set; } // Giá trị cũ

    [Column("new_value")]
    [StringLength(500)]
    public string? NewValue { get; set; } // Giá trị mới

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("valid_from")]
    [Precision(3)]
    public DateTime? ValidFrom { get; set; } // Thời gian bắt đầu có hiệu lực

    [Column("valid_to")]
    [Precision(3)]
    public DateTime? ValidTo { get; set; } // Thời gian kết thúc hiệu lực

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("is_delete")]
    public bool IsDelete { get; set; } = false;

    [ForeignKey("CardId")]
    [InverseProperty("CardHistories")]
    public virtual AccessCard Card { get; set; } = null!;

}
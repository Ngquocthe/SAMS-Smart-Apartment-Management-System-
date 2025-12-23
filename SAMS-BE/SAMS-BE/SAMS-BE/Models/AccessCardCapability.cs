using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("access_card_capabilities", Schema = "building")]
public partial class AccessCardCapability
{
    [Key]
    [Column("card_capability_id")]
    public Guid CardCapabilityId { get; set; }

    [Column("card_id")]
    public Guid CardId { get; set; }

    [Column("card_type_id")]
    public Guid CardTypeId { get; set; }

    [Column("is_enabled")]
    public bool IsEnabled { get; set; }

    [Column("valid_from")]
    [Precision(3)]
    public DateTime? ValidFrom { get; set; }

    [Column("valid_to")]
    [Precision(3)]
    public DateTime? ValidTo { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [Column("updated_by")]
    [StringLength(190)]
    public string? UpdatedBy { get; set; }

    [ForeignKey("CardId")]
    [InverseProperty("AccessCardCapabilities")]
    public virtual AccessCard Card { get; set; } = null!;

    [ForeignKey("CardTypeId")]
    [InverseProperty("AccessCardCapabilities")]
    public virtual AccessCardType CardType { get; set; } = null!;
}

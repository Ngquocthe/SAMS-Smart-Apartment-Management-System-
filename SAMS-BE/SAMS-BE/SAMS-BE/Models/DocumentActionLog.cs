using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("document_action_log", Schema = "building")]
public partial class DocumentActionLog
{
    [Key]
    [Column("action_log_id")]
    public Guid ActionLogId { get; set; }

    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Column("action")]
    [StringLength(64)]
    public string Action { get; set; } = null!;

    [Column("actor_id")]
    public Guid? ActorId { get; set; }

    [Column("action_at")]
    [Precision(3)]
    public DateTime ActionAt { get; set; }

    [Column("detail")]
    [StringLength(1000)]
    public string? Detail { get; set; }

    [ForeignKey("DocumentId")]
    [InverseProperty("DocumentActionLogs")]
    public virtual Document Document { get; set; } = null!;
}

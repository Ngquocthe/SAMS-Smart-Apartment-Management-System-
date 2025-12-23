using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("documents", Schema = "building")]
public partial class Document
{
    [Key]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Column("category")]
    [StringLength(64)]
    public string Category { get; set; } = null!;

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("visibility_scope")]
    [StringLength(120)]
    public string? VisibilityScope { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("current_version")]
    public int? CurrentVersion { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [Column("is_delete")]
    public bool IsDelete { get; set; }

    [InverseProperty("Document")]
    public virtual ICollection<DocumentActionLog> DocumentActionLogs { get; set; } = new List<DocumentActionLog>();

    [InverseProperty("Document")]
    public virtual ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("document_versions", Schema = "building")]
[Index("DocumentId", "VersionNo", Name = "UQ_document_versions", IsUnique = true)]
public partial class DocumentVersion
{
    [Key]
    [Column("document_version_id")]
    public Guid DocumentVersionId { get; set; }

    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Column("version_no")]
    public int VersionNo { get; set; }

    [Column("file_id")]
    public Guid FileId { get; set; }

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    [Column("changed_at")]
    [Precision(3)]
    public DateTime ChangedAt { get; set; }

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [ForeignKey("DocumentId")]
    [InverseProperty("DocumentVersions")]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey("FileId")]
    [InverseProperty("DocumentVersions")]
    public virtual File File { get; set; } = null!;
}

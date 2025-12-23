using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("files", Schema = "building")]
public partial class File
{
    [Key]
    [Column("file_id")]
    public Guid FileId { get; set; }

    [Column("original_name")]
    [StringLength(255)]
    public string OriginalName { get; set; } = null!;

    [Column("mime_type")]
    [StringLength(128)]
    public string MimeType { get; set; } = null!;

    [Column("storage_path")]
    [StringLength(1000)]
    public string StoragePath { get; set; } = null!;

    [Column("uploaded_by")]
    [StringLength(190)]
    public string? UploadedBy { get; set; }

    [Column("uploaded_at")]
    [Precision(3)]
    public DateTime UploadedAt { get; set; }

    [InverseProperty("File")]
    public virtual ICollection<TicketAttachment> TicketAttachments { get; set; } = new List<TicketAttachment>();

    [InverseProperty("File")]
    public virtual ICollection<DocumentVersion> DocumentVersions { get; set; } = new List<DocumentVersion>();
}
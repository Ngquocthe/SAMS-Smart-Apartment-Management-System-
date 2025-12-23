using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("ticket_attachments", Schema = "building")]
public partial class TicketAttachment
{
    [Key]
    [Column("attachment_id")]
    public Guid AttachmentId { get; set; }

    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Column("file_id")]
    public Guid FileId { get; set; }

    [Column("uploaded_by")]
    public Guid? UploadedBy { get; set; }

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    [Column("uploaded_at")]
    [Precision(3)]
    public DateTime UploadedAt { get; set; }

    [ForeignKey("FileId")]
    [InverseProperty("TicketAttachments")]
    public virtual File File { get; set; } = null!;

    [ForeignKey("TicketId")]
    [InverseProperty("TicketAttachments")]
    public virtual Ticket Ticket { get; set; } = null!;

}

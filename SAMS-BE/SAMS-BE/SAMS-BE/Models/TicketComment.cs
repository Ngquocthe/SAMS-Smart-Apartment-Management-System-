using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("ticket_comments", Schema = "building")]
public partial class TicketComment
{
    [Key]
    [Column("comment_id")]
    public Guid CommentId { get; set; }

    [Column("ticket_id")]
    public Guid TicketId { get; set; }

    [Column("commented_by")]
    public Guid? CommentedBy { get; set; }

    [Column("comment_time")]
    [Precision(3)]
    public DateTime CommentTime { get; set; }

    [Column("content")]
    public string Content { get; set; } = null!;

    [ForeignKey("TicketId")]
    [InverseProperty("TicketComments")]
    public virtual Ticket Ticket { get; set; } = null!;

}

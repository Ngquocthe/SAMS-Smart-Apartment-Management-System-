using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Infrastructure.Persistence.Global.Models;

[Table("announcement_global")]
[Index("status", "schedule_start", "schedule_end", Name = "IX_announcement_status_time")]
public partial class announcement_global
{
    [Key]
    public Guid id { get; set; }

    [StringLength(200)]
    public string title { get; set; } = null!;

    public string content { get; set; } = null!;

    [StringLength(500)]
    public string? targets { get; set; }

    [Precision(3)]
    public DateTime? schedule_start { get; set; }

    [Precision(3)]
    public DateTime? schedule_end { get; set; }

    public byte status { get; set; }

    public Guid? created_by { get; set; }

    [Precision(3)]
    public DateTime created_at { get; set; }

    [Precision(3)]
    public DateTime? updated_at { get; set; }

    [ForeignKey("created_by")]
    [InverseProperty("announcement_globals")]
    public virtual user_registry? created_byNavigation { get; set; }
}

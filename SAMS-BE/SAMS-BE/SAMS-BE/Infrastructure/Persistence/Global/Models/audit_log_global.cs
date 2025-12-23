using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Infrastructure.Persistence.Global.Models;

[Table("audit_log_global")]
[Index("actor_keycloak_id", Name = "IX_audit_actor")]
[Index("building_id", Name = "IX_audit_building")]
[Index("created_at", Name = "IX_audit_created_at")]
public partial class audit_log_global
{
    [Key]
    public long id { get; set; }

    public Guid? actor_keycloak_id { get; set; }

    [StringLength(100)]
    public string action { get; set; } = null!;

    [StringLength(50)]
    public string? target_type { get; set; }

    public Guid? building_id { get; set; }

    [Precision(3)]
    public DateTime? schedule_end { get; set; }

    public string? payload { get; set; }

    [StringLength(45)]
    [Unicode(false)]
    public string? ip { get; set; }

    [StringLength(255)]
    public string? ua { get; set; }

    [Precision(3)]
    public DateTime created_at { get; set; }

    [ForeignKey("actor_keycloak_id")]
    [InverseProperty("audit_log_globals")]
    public virtual user_registry? actor_keycloak { get; set; }

    [ForeignKey("building_id")]
    [InverseProperty("audit_log_globals")]
    public virtual building? building { get; set; }
}

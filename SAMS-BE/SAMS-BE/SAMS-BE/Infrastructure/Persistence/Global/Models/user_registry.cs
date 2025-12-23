using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Infrastructure.Persistence.Global.Models;

[Table("user_registry")]
[Index("email", Name = "UQ_user_registry_email", IsUnique = true)]
[Index("keycloak_user_id", Name = "UQ_user_registry_keycloak", IsUnique = true)]
[Index("username", Name = "UQ_user_registry_username", IsUnique = true)]
public partial class user_registry
{
    [Key]
    public Guid id { get; set; }

    public Guid keycloak_user_id { get; set; }

    [StringLength(50)]
    public string username { get; set; } = null!;

    [StringLength(100)]
    public string email { get; set; } = null!;

    public byte status { get; set; }

    [Precision(3)]
    public DateTime create_at { get; set; }

    [Precision(3)]
    public DateTime? update_at { get; set; }

    [InverseProperty("created_byNavigation")]
    public virtual ICollection<announcement_global> announcement_globals { get; set; } = new List<announcement_global>();

    [InverseProperty("actor_keycloak")]
    public virtual ICollection<audit_log_global> audit_log_globals { get; set; } = new List<audit_log_global>();

    [InverseProperty("keycloak_user")]
    public virtual ICollection<user_building> user_buildings { get; set; } = new List<user_building>();
}

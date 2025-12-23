using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Infrastructure.Persistence.Global.Models;

[Table("user_building")]
[Index("building_id", Name = "IX_user_building_building")]
[Index("keycloak_user_id", Name = "IX_user_building_user")]
[Index("keycloak_user_id", "building_id", Name = "UQ_user_building_user_building", IsUnique = true)]
public partial class user_building
{
    [Key]
    public Guid id { get; set; }

    public Guid keycloak_user_id { get; set; }

    public Guid building_id { get; set; }

    public byte status { get; set; }

    [Precision(3)]
    public DateTime create_at { get; set; }

    [Precision(3)]
    public DateTime? update_at { get; set; }

    [ForeignKey("building_id")]
    [InverseProperty("user_buildings")]
    public virtual building building { get; set; } = null!;

    [ForeignKey("keycloak_user_id")]
    [InverseProperty("user_buildings")]
    public virtual user_registry keycloak_user { get; set; } = null!;
}

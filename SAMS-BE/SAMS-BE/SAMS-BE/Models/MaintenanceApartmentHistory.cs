using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("maintenance_apartment_history", Schema = "building")]
public partial class MaintenanceApartmentHistory
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("creator_user_id")]
    public Guid CreatorUserId { get; set; }

    [Column("handler_user_id")]
    public Guid? HandlerUserId { get; set; }

    [Column("request_id")]
    public Guid? RequestId { get; set; }

    [Column("request_type")]
    [StringLength(64)]
    public string RequestType { get; set; } = null!;

    [Column("target_department")]
    [StringLength(128)]
    public string? TargetDepartment { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("creation_time")]
    [Precision(3)]
    public DateTime CreationTime { get; set; }

    [Column("priority")]
    [StringLength(32)]
    public string Priority { get; set; } = null!;

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("attachment")]
    [StringLength(1000)]
    public string? Attachment { get; set; }

    [Column("sla_due_time")]
    [Precision(3)]
    public DateTime? SlaDueTime { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("MaintenanceApartmentHistories")]
    public virtual Apartment Apartment { get; set; } = null!;

    [ForeignKey("CreatorUserId")]
    [InverseProperty("MaintenanceApartmentHistoryCreatorUsers")]
    public virtual User CreatorUser { get; set; } = null!;

    [ForeignKey("HandlerUserId")]
    [InverseProperty("MaintenanceApartmentHistoryHandlerUsers")]
    public virtual User? HandlerUser { get; set; }

    [ForeignKey("RequestId")]
    [InverseProperty("MaintenanceApartmentHistories")]
    public virtual Ticket? Request { get; set; }
}

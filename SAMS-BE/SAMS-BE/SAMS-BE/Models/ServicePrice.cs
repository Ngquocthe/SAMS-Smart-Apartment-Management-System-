using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("service_prices", Schema = "building")]
public partial class ServicePrice
{
    [Key]
    [Column("service_prices")]
    public Guid ServicePrices { get; set; }

    [Column("service_type_id")]
    public Guid ServiceTypeId { get; set; }

    [Column("unit_price", TypeName = "decimal(18, 6)")]
    public decimal UnitPrice { get; set; }

    [Column("effective_date")]
    public DateOnly EffectiveDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("approved_by")]
    public Guid? ApprovedBy { get; set; }

    [Column("approved_date")]
    [Precision(3)]
    public DateTime? ApprovedDate { get; set; }

    [Column("notes")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey("ApprovedBy")]
    [InverseProperty("ServicePriceApprovedByNavigations")]
    public virtual StaffProfile? ApprovedByNavigation { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("ServicePriceCreatedByNavigations")]
    public virtual StaffProfile? CreatedByNavigation { get; set; }

    [ForeignKey("ServiceTypeId")]
    [InverseProperty("ServicePrices")]
    public virtual ServiceType ServiceType { get; set; } = null!;
}

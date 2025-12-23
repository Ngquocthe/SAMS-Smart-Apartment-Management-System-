using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("apartment_services", Schema = "building")]
[Index("ApartmentId", "ServiceId", "StartDate", Name = "UQ_apartment_services", IsUnique = true)]
public partial class ApartmentService
{
    [Key]
    [Column("apartment_service_id")]
    public Guid ApartmentServiceId { get; set; }

    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("service_id")]
    public Guid ServiceId { get; set; }

    [Column("start_date")]
    public DateOnly StartDate { get; set; }

    [Column("end_date")]
    public DateOnly? EndDate { get; set; }

    [Column("billing_cycle")]
    [StringLength(16)]
    public string BillingCycle { get; set; } = null!;

    [Column("quantity", TypeName = "decimal(18, 4)")]
    public decimal? Quantity { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("meta")]
    public string? Meta { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("ApartmentServices")]
    public virtual Apartment Apartment { get; set; } = null!;

    [ForeignKey("ServiceId")]
    [InverseProperty("ApartmentServices")]
    public virtual ServiceType Service { get; set; } = null!;
}

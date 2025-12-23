using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("service_types", Schema = "building")]
[Index("Code", Name = "UQ_service_types_code", IsUnique = true)]
public partial class ServiceType
{
    [Key]
    [Column("service_type_id")]
    public Guid ServiceTypeId { get; set; }

    [Column("code")]
    [StringLength(64)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("category_id")]
    public Guid CategoryId { get; set; }

    [Column("unit")]
    [StringLength(64)]
    public string? Unit { get; set; }

    [Column("is_mandatory")]
    public bool IsMandatory { get; set; }

    [Column("is_recurring")]
    public bool IsRecurring { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_delete")]
    public bool? IsDelete { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [InverseProperty("Service")]
    public virtual ICollection<ApartmentService> ApartmentServices { get; set; } = new List<ApartmentService>();

    [InverseProperty("Service")]
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    [InverseProperty("Service")]
    public virtual ICollection<Meter> Meters { get; set; } = new List<Meter>();

    [InverseProperty("ServiceType")]
    public virtual ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();

    [InverseProperty("ServiceType")]
    public virtual ICollection<VoucherItem> VoucherItems { get; set; } = new List<VoucherItem>();

    [ForeignKey(nameof(CategoryId))]
    [InverseProperty(nameof(ServiceTypeCategory.ServiceTypes))]
    public ServiceTypeCategory Category { get; set; } = default!;
}
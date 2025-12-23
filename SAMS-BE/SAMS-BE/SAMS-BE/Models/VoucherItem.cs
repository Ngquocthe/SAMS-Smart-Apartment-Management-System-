using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("voucher_items", Schema = "building")]
public partial class VoucherItem
{
    [Key]
    [Column("voucher_items_id")]
    public Guid VoucherItemsId { get; set; }

    [Column("voucher_id")]
    public Guid VoucherId { get; set; }

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("quantity", TypeName = "decimal(18, 2)")]
    public decimal? Quantity { get; set; }

    [Column("unit_price", TypeName = "decimal(18, 2)")]
    public decimal? UnitPrice { get; set; }

    [Column("amount", TypeName = "decimal(18, 2)")]
    public decimal? Amount { get; set; }

    [Column("service_type_id")]
    public Guid? ServiceTypeId { get; set; }

    [Column("apartment_id")]
    public Guid? ApartmentId { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("VoucherItems")]
    public virtual Apartment? Apartment { get; set; }

    [ForeignKey("ServiceTypeId")]
    [InverseProperty("VoucherItems")]
    public virtual ServiceType? ServiceType { get; set; }

    [ForeignKey("VoucherId")]
    [InverseProperty("VoucherItems")]
    public virtual Voucher Voucher { get; set; } = null!;
}

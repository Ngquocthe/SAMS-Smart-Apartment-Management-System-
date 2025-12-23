using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("invoice_details", Schema = "building")]
public partial class InvoiceDetail
{
    [Key]
    [Column("invoice_detail_id")]
    public Guid InvoiceDetailId { get; set; }

    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("service_id")]
    public Guid ServiceId { get; set; }

    [Column("description")]
    [StringLength(255)]
    public string? Description { get; set; }

    [Column("quantity", TypeName = "decimal(18, 6)")]
    public decimal Quantity { get; set; }

    [Column("unit_price", TypeName = "decimal(18, 6)")]
    public decimal UnitPrice { get; set; }

    [Column("amount", TypeName = "decimal(37, 12)")]
    public decimal? Amount { get; set; }

    [Column("vat_rate", TypeName = "decimal(5, 2)")]
    public decimal? VatRate { get; set; }

    [Column("vat_amount", TypeName = "decimal(18, 2)")]
    public decimal? VatAmount { get; set; }

    [ForeignKey("InvoiceId")]
    [InverseProperty("InvoiceDetails")]
    public virtual Invoice Invoice { get; set; } = null!;

    [ForeignKey("ServiceId")]
    [InverseProperty("InvoiceDetails")]
    public virtual ServiceType Service { get; set; } = null!;
}


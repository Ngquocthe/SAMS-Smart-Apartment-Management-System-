using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("receipts", Schema = "building")]
[Index("InvoiceId", Name = "UQ_receipts_invoice", IsUnique = true)]
[Index("ReceiptNo", Name = "UQ_receipts_no", IsUnique = true)]
public partial class Receipt
{
    [Key]
    [Column("receipt_id")]
    public Guid ReceiptId { get; set; }

    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("receipt_no")]
    [StringLength(64)]
    public string ReceiptNo { get; set; } = null!;

    [Column("received_date")]
    [Precision(3)]
    public DateTime ReceivedDate { get; set; }

    [Column("method_id")]
    public Guid MethodId { get; set; }

    [Column("amount_total", TypeName = "decimal(18, 2)")]
    public decimal AmountTotal { get; set; }

    [Column("note")]
    [StringLength(1000)]
    public string? Note { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; } // ✅ Nullable for system-generated receipts

    [ForeignKey("CreatedBy")]
    [InverseProperty("Receipts")]
    public virtual User? CreatedByNavigation { get; set; } // ✅ Nullable navigation

    [ForeignKey("InvoiceId")]
    [InverseProperty("Receipt")]
    public virtual Invoice Invoice { get; set; } = null!;

    [ForeignKey("MethodId")]
    [InverseProperty("Receipts")]
    public virtual PaymentMethod Method { get; set; } = null!;
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("invoices", Schema = "building")]
[Index("ApartmentId", "DueDate", Name = "IX_invoices_apartment_due")]
[Index("InvoiceNo", Name = "UQ_invoices_no", IsUnique = true)]
public partial class Invoice
{
    [Key]
    [Column("invoice_id")]
    public Guid InvoiceId { get; set; }

    [Column("invoice_no")]
    [StringLength(64)]
    public string InvoiceNo { get; set; } = null!;

    [Column("apartment_id")]
    public Guid ApartmentId { get; set; }

    [Column("issue_date")]
    public DateOnly IssueDate { get; set; }

    [Column("due_date")]
    public DateOnly DueDate { get; set; }

    [Column("status")]
    [StringLength(32)]
    public string Status { get; set; } = null!;

    [Column("subtotal_amount", TypeName = "decimal(18, 2)")]
    public decimal SubtotalAmount { get; set; }

    [Column("tax_amount", TypeName = "decimal(18, 2)")]
    public decimal TaxAmount { get; set; }

    [Column("total_amount", TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column("note")]
    [StringLength(1000)]
    public string? Note { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("created_by")]
    [StringLength(190)]
    public string? CreatedBy { get; set; }

    [Column("updated_at")]
    [Precision(3)]
    public DateTime? UpdatedAt { get; set; }

    [Column("updated_by")]
    [StringLength(190)]
    public string? UpdatedBy { get; set; }

    [Column("ticket_id")]
    public Guid? TicketId { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("Invoices")]
    public virtual Apartment Apartment { get; set; } = null!;

    [ForeignKey("TicketId")]
    [InverseProperty("Invoices")]
    public virtual Ticket? Ticket { get; set; }

    [InverseProperty("Invoice")]
    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    [InverseProperty("Invoice")]
    public virtual Receipt? Receipt { get; set; }
}

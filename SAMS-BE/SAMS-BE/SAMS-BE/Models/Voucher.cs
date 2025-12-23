using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("vouchers", Schema = "building")]
[Index("VoucherNumber", Name = "UQ_vouchers_number", IsUnique = true)]
public partial class Voucher
{
    [Key]
    [Column("voucher_id")]
    public Guid VoucherId { get; set; }

    [Column("voucher_number")]
    [StringLength(64)]
    public string VoucherNumber { get; set; } = null!;

    [Column("company_info")]
    public string? CompanyInfo { get; set; }

    [Column("type")]
    [StringLength(32)]
    public string Type { get; set; } = null!;

    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("total_amount", TypeName = "decimal(18, 2)")]
    public decimal TotalAmount { get; set; }

    [Column("description")]
    [StringLength(1000)]
    public string? Description { get; set; }

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

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("ticket_id")]
    public Guid? TicketId { get; set; }

    [Column("history_id")]
    public Guid? HistoryId { get; set; }

    [ForeignKey("ApprovedBy")]
    [InverseProperty("VoucherApprovedByNavigations")]
    public virtual StaffProfile? ApprovedByNavigation { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("VoucherCreatedByNavigations")]
    public virtual StaffProfile? CreatedByNavigation { get; set; }

    [ForeignKey("TicketId")]
    [InverseProperty("Vouchers")]
    public virtual Ticket? Ticket { get; set; }

    [ForeignKey("HistoryId")]
    [InverseProperty("Vouchers")]
    public virtual AssetMaintenanceHistory? History { get; set; }

    [InverseProperty("Voucher")]
    public virtual ICollection<VoucherItem> VoucherItems { get; set; } = new List<VoucherItem>();
}

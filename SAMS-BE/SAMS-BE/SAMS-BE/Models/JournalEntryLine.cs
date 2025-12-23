using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("journal_entry_lines", Schema = "building")]
[Index("EntryId", "LineNumber", Name = "UQ_jel_entry_line", IsUnique = true)]
public partial class JournalEntryLine
{
    [Key]
    [Column("line_id")]
    public Guid LineId { get; set; }

    [Column("entry_id")]
    public Guid EntryId { get; set; }

    [Column("line_number")]
    public int LineNumber { get; set; }

    [Column("account_code")]
    [StringLength(64)]
    public string AccountCode { get; set; } = null!;

    [Column("description")]
    [StringLength(500)]
    public string? Description { get; set; }

    [Column("debit_amount", TypeName = "decimal(18, 2)")]
    public decimal? DebitAmount { get; set; }

    [Column("credit_amount", TypeName = "decimal(18, 2)")]
    public decimal? CreditAmount { get; set; }

    [Column("apartment_id")]
    public Guid? ApartmentId { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ApartmentId")]
    [InverseProperty("JournalEntryLines")]
    public virtual Apartment? Apartment { get; set; }

    [ForeignKey("EntryId")]
    [InverseProperty("JournalEntryLines")]
    public virtual JournalEntry Entry { get; set; } = null!;
}

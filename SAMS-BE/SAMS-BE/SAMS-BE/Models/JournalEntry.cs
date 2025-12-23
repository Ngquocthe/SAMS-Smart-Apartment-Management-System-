using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Models;

[Table("journal_entries", Schema = "building")]
[Index("EntryNumber", Name = "UQ_journal_entries_number", IsUnique = true)]
public partial class JournalEntry
{
    [Key]
    [Column("entry_id")]
    public Guid EntryId { get; set; }

    [Column("entry_number")]
    [StringLength(64)]
    public string EntryNumber { get; set; } = null!;

    [Column("entry_type")]
    [StringLength(50)]
    public string? EntryType { get; set; }

    [Column("entry_date")]
    public DateOnly EntryDate { get; set; }

    [Column("reference_type")]
    [StringLength(32)]
    public string? ReferenceType { get; set; }

    [Column("reference_id")]
    public Guid? ReferenceId { get; set; }

    [Column("description")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Column("status")]
    [StringLength(16)]
    public string Status { get; set; } = null!;

    [Column("posted_by")]
    public Guid? PostedBy { get; set; }

    [Column("posted_date")]
    [Precision(3)]
    public DateTime? PostedDate { get; set; }

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    [Precision(3)]
    public DateTime CreatedAt { get; set; }

    [Column("fiscal_period")]
    [StringLength(20)]
    public string FiscalPeriod { get; set; } = null!;

    [ForeignKey("CreatedBy")]
    [InverseProperty("JournalEntryCreatedByNavigations")]
    public virtual StaffProfile? CreatedByNavigation { get; set; }

    [InverseProperty("Entry")]
    public virtual ICollection<JournalEntryLine> JournalEntryLines { get; set; } = new List<JournalEntryLine>();

    [ForeignKey("PostedBy")]
    [InverseProperty("JournalEntryPostedByNavigations")]
    public virtual StaffProfile? PostedByNavigation { get; set; }
}

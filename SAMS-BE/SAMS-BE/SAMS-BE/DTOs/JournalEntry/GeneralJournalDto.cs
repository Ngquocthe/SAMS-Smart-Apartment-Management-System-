namespace SAMS_BE.DTOs.JournalEntry
{
    /// <summary>
    /// Response for General Journal (S? nh?t ký chung)
    /// </summary>
    public class GeneralJournalDto
    {
        public Guid EntryId { get; set; }
        public string EntryNumber { get; set; } = null!;
        public DateOnly EntryDate { get; set; }
        public string? EntryType { get; set; }
        public string? ReferenceType { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? FiscalPeriod { get; set; }
        public Guid? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? PostedDate { get; set; }

        public List<JournalEntryLineDto> Lines { get; set; } = new();

        public decimal TotalDebit => Lines.Sum(l => l.DebitAmount ?? 0);
        public decimal TotalCredit => Lines.Sum(l => l.CreditAmount ?? 0);
        public bool IsBalanced => TotalDebit == TotalCredit;
    }

    public class JournalEntryLineDto
    {
        public Guid LineId { get; set; }
        public int LineNumber { get; set; }
        public string AccountCode { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public Guid? ApartmentId { get; set; }
        public string? ApartmentNumber { get; set; }
    }
}

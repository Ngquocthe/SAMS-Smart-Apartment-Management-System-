namespace SAMS_BE.DTOs.JournalEntry
{
    /// <summary>
    /// Query parameters for journal entries (General Journal)
    /// </summary>
    public class JournalEntryQueryDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? EntryType { get; set; }  // RECEIPT, VOUCHER
        public string? Status { get; set; }      // DRAFT, POSTED
        public string? Search { get; set; }      // Search in entry_number, description
        public string? FiscalPeriod { get; set; } // YYYY-MM
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string SortBy { get; set; } = "EntryDate";
        public string SortDir { get; set; } = "desc";
    }
}

namespace SAMS_BE.DTOs.JournalEntry
{
    /// <summary>
    /// General Ledger by Account (S? cái theo tài kho?n)
    /// </summary>
    public class GeneralLedgerDto
    {
        public string AccountCode { get; set; } = null!;
        public string AccountName { get; set; } = null!;
        public string FiscalPeriod { get; set; } = null!;
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }

        public List<LedgerTransactionDto> Transactions { get; set; } = new();
    }

    public class LedgerTransactionDto
    {
        public DateOnly Date { get; set; }
        public string EntryNumber { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal RunningBalance { get; set; }
        public Guid EntryId { get; set; }
    }
}

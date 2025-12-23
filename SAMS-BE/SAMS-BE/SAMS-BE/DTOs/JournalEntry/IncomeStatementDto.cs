namespace SAMS_BE.DTOs.JournalEntry
{
    /// <summary>
    /// Income Statement (Báo cáo thu chi)
    /// </summary>
    public class IncomeStatementDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // REVENUES (Doanh thu)
        public List<RevenueItemDto> RevenueItems { get; set; } = new();
        public decimal TotalRevenue => RevenueItems.Sum(r => r.Amount);

        // EXPENSES (Chi phí)
        public List<ExpenseItemDto> ExpenseItems { get; set; } = new();
        public decimal TotalExpense => ExpenseItems.Sum(e => e.Amount);

        // PROFIT/LOSS
        public decimal NetProfit => TotalRevenue - TotalExpense;
    }

    public class RevenueItemDto
    {
        public string Category { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? AccountCode { get; set; }
    }

    public class ExpenseItemDto
    {
        public string Category { get; set; } = null!;
        public decimal Amount { get; set; }
        public string? AccountCode { get; set; }
    }
}

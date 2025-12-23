namespace SAMS_BE.DTOs.JournalEntry
{
    /// <summary>
    /// Balance Sheet (B?ng cân ??i k? toán)
    /// </summary>
    public class BalanceSheetDto
    {
        public string FiscalPeriod { get; set; } = null!;
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // ASSETS (Tài s?n)
        public List<AccountBalanceDto> Assets { get; set; } = new();
        public decimal TotalAssets => Assets.Sum(a => a.Balance);

        // REVENUE (Doanh thu)
        public List<AccountBalanceDto> Revenues { get; set; } = new();
        public decimal TotalRevenues => Revenues.Sum(r => r.Balance);

        // EXPENSES (Chi phí)
        public List<AccountBalanceDto> Expenses { get; set; } = new();
        public decimal TotalExpenses => Expenses.Sum(e => e.Balance);

        // PROFIT/LOSS (Lãi/L?)
        public decimal ProfitLoss => TotalRevenues - TotalExpenses;
    }

    public class AccountBalanceDto
    {
        public string AccountCode { get; set; } = null!;
        public string AccountName { get; set; } = null!;
        public decimal Balance { get; set; }
    }
}

namespace SAMS_BE.DTOs.JournalEntry
{
    /// <summary>
    /// Financial Dashboard (Dashboard tai chinh)
    /// </summary>
    public class FinancialDashboardDto
    {
        public string Period { get; set; } = null!;  // "today", "week", "month", "custom"
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // KPI Cards
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }  // % compared to previous period

        public decimal TotalExpense { get; set; }
        public decimal ExpenseGrowth { get; set; }

        public decimal NetProfit { get; set; }
        public decimal ProfitGrowth { get; set; }

        // Cash Position
        public decimal CashBalance { get; set; }       // Account 1111
        public decimal BankBalance { get; set; }       // Account 1121
        public decimal TotalCash => CashBalance + BankBalance;

        // Charts Data
        public List<ChartDataPoint> RevenueTrend { get; set; } = new();
        public List<ChartDataPoint> ExpenseTrend { get; set; } = new();
        public List<PieChartData> RevenueBreakdown { get; set; } = new();
        public List<TopExpenseItem> TopExpenses { get; set; } = new();
        public List<SourceAmountDto> TopRevenueSources { get; set; } = new();
        public List<SourceAmountDto> TopExpenseSources { get; set; } = new();
    }

    public class ChartDataPoint
    {
        public string Label { get; set; } = null!;  // Date or category
        public decimal Value { get; set; }
    }

    public class PieChartData
    {
        public string Category { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }

    public class TopExpenseItem
    {
        public string Category { get; set; } = null!;
        public decimal Amount { get; set; }
    }

    public class SourceAmountDto
    {
        public string Source { get; set; } = null!;
        public decimal Amount { get; set; }
        public decimal Percentage { get; set; }
    }
}
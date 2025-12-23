using Microsoft.EntityFrameworkCore;
using SAMS_BE.DTOs.JournalEntry;
using SAMS_BE.Helpers;

namespace SAMS_BE.Services
{
    /// <summary>
    /// Journal Entry Service - Financial Statements
    /// </summary>
    public partial class JournalEntryService
    {
        /// <summary>
        /// Get Income Statement (Báo cáo thu chi)
        /// </summary>
        public async Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime from, DateTime to)
        {
            try
            {
                var fromDate = DateOnly.FromDateTime(from);
                var toDate = DateOnly.FromDateTime(to);

                // Get all posted journal entry lines in date range
                var lines = await _context.JournalEntryLines
                .Include(jel => jel.Entry)
                     .Where(jel => jel.Entry.EntryDate >= fromDate &&
       jel.Entry.EntryDate <= toDate &&
           jel.Entry.Status == JournalEntryHelper.STATUS_POSTED)
                .ToListAsync();

                // Group revenues (5xxx accounts)
                var revenueLines = lines
       .Where(l => l.AccountCode.StartsWith("5"))
        .GroupBy(l => l.AccountCode)
                   .Select(g => new RevenueItemDto
                   {
                       Category = GetAccountName(g.Key),
                       AccountCode = g.Key,
                       Amount = g.Sum(l => Math.Abs((l.CreditAmount ?? 0) - (l.DebitAmount ?? 0)))
                   })
         .Where(r => r.Amount > 0)
                  .OrderByDescending(r => r.Amount)
        .ToList();

                // ✅ Bổ sung: Tính doanh thu từ Amenity Bookings đã thanh toán thành công
                var amenityRevenue = await _context.AmenityBookings
 .Where(ab => ab.StartDate >= fromDate &&
  ab.StartDate <= toDate &&
                ab.PaymentStatus == "Paid" &&
      (ab.Status == "Completed" || ab.Status == "Confirmed") &&
              !ab.IsDelete)
            .SumAsync(ab => ab.TotalPrice);

                // Thêm doanh thu từ amenity vào danh sách
                if (amenityRevenue > 0)
                {
                    revenueLines.Add(new RevenueItemDto
                    {
                        Category = "Doanh thu từ tiện ích (Amenity Bookings)",
                        AccountCode = "5200",
                        Amount = amenityRevenue
                    });
                }

                // Sort lại sau khi thêm
                revenueLines = revenueLines
                .OrderByDescending(r => r.Amount)
                         .ToList();

                // Group expenses (6xxx accounts)
                var expenseLines = lines
           .Where(l => l.AccountCode.StartsWith("6"))
         .GroupBy(l => l.AccountCode)
      .Select(g => new ExpenseItemDto
      {
          Category = GetAccountName(g.Key),
          AccountCode = g.Key,
          Amount = g.Sum(l => Math.Abs((l.DebitAmount ?? 0) - (l.CreditAmount ?? 0)))
      })
      .Where(e => e.Amount > 0)
     .OrderByDescending(e => e.Amount)
       .ToList();

                return new IncomeStatementDto
                {
                    FromDate = from,
                    ToDate = to,
                    GeneratedAt = DateTime.UtcNow,
                    RevenueItems = revenueLines,
                    ExpenseItems = expenseLines
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting income statement from {from} to {to}");
                throw;
            }
        }

        /// <summary>
        /// Get Financial Dashboard
        /// </summary>
        public async Task<FinancialDashboardDto> GetFinancialDashboardAsync(string period)
        {
            try
            {
                // Calculate date range based on period
                DateTime fromDate, toDate;
                DateTime previousFromDate, previousToDate;

                switch (period.ToLower())
                {
                    case "month":
                    default:
                        fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                        toDate = fromDate.AddMonths(1).AddMilliseconds(-1);
                        previousFromDate = fromDate.AddMonths(-1);
                        previousToDate = fromDate.AddMilliseconds(-1);
                        break;

                    case "quarter":
                        // Tính quý hiện tại (Q1: 1-3, Q2: 4-6, Q3: 7-9, Q4: 10-12)
                        var currentQuarter = (DateTime.Today.Month - 1) / 3;
                        var quarterStartMonth = currentQuarter * 3 + 1;
                        fromDate = new DateTime(DateTime.Today.Year, quarterStartMonth, 1);
                        toDate = fromDate.AddMonths(3).AddMilliseconds(-1);

                        // Quý trước
                        previousFromDate = fromDate.AddMonths(-3);
                        previousToDate = fromDate.AddMilliseconds(-1);
                        break;

                    case "year":
                        fromDate = new DateTime(DateTime.Today.Year, 1, 1);
                        toDate = new DateTime(DateTime.Today.Year, 12, 31, 23, 59, 59, 999);

                        // Năm trước
                        previousFromDate = new DateTime(DateTime.Today.Year - 1, 1, 1);
                        previousToDate = new DateTime(DateTime.Today.Year - 1, 12, 31, 23, 59, 59, 999);
                        break;
                }

                // Get current period data
                var currentIncome = await GetIncomeStatementAsync(fromDate, toDate);

                // Get previous period data for growth calculation
                var previousIncome = await GetIncomeStatementAsync(previousFromDate, previousToDate);

                // Calculate growth percentages
                var revenueGrowth = CalculateGrowth(previousIncome.TotalRevenue, currentIncome.TotalRevenue);
                var expenseGrowth = CalculateGrowth(previousIncome.TotalExpense, currentIncome.TotalExpense);
                var profitGrowth = CalculateGrowth(previousIncome.NetProfit, currentIncome.NetProfit);

                // Get cash balances
                var cashBalance = await GetAccountBalance("1111", toDate);
                var bankBalance = await GetAccountBalance("1121", toDate);

                // Create dashboard
                var dashboard = new FinancialDashboardDto
                {
                    Period = period,
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalRevenue = currentIncome.TotalRevenue,
                    RevenueGrowth = revenueGrowth,
                    TotalExpense = currentIncome.TotalExpense,
                    ExpenseGrowth = expenseGrowth,
                    NetProfit = currentIncome.NetProfit,
                    ProfitGrowth = profitGrowth,
                    CashBalance = cashBalance,
                    BankBalance = bankBalance,
                    RevenueBreakdown = currentIncome.RevenueItems.Select(r => new PieChartData
                    {
                        Category = r.Category,
                        Amount = r.Amount,
                        Percentage = currentIncome.TotalRevenue > 0 ? (r.Amount / currentIncome.TotalRevenue * 100) : 0
                    }).ToList(),
                    TopExpenses = currentIncome.ExpenseItems.Take(5).Select(e => new TopExpenseItem
                    {
                        Category = e.Category,
                        Amount = e.Amount
                    }).ToList(),
                    TopRevenueSources = BuildTopSources(currentIncome.RevenueItems.Select(r => (Source: r.Category, Amount: r.Amount)), currentIncome.TotalRevenue),
                    TopExpenseSources = BuildTopSources(currentIncome.ExpenseItems.Select(e => (Source: e.Category, Amount: e.Amount)), currentIncome.TotalExpense)
                };

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting financial dashboard for period {period}");
                throw;
            }
        }

        /// <summary>
        /// Helper: Get account balance at specific date
        /// </summary>
        private async Task<decimal> GetAccountBalance(string accountCode, DateTime asOfDate)
        {
            var asOfDateOnly = DateOnly.FromDateTime(asOfDate);

            var balance = await _context.JournalEntryLines
                   .Include(jel => jel.Entry)
                      .Where(jel => jel.AccountCode == accountCode &&
                     jel.Entry.EntryDate <= asOfDateOnly &&
                       jel.Entry.Status == JournalEntryHelper.STATUS_POSTED)
             .SumAsync(jel => (jel.DebitAmount ?? 0) - (jel.CreditAmount ?? 0));

            return balance;
        }

        /// <summary>
        /// Helper: Calculate growth percentage
        /// </summary>
        private decimal CalculateGrowth(decimal previous, decimal current)
        {
            if (previous == 0) return current > 0 ? 100 : 0;
            return Math.Round(((current - previous) / Math.Abs(previous)) * 100, 2);
        }

        /// <summary>
        /// Helper: Determine account type from code
        /// </summary>
        private string GetAccountType(string accountCode)
        {
            if (accountCode.StartsWith("1")) return "ASSET";
            if (accountCode.StartsWith("5")) return "REVENUE";
            if (accountCode.StartsWith("6")) return "EXPENSE";
            return "OTHER";
        }

        private static List<SourceAmountDto> BuildTopSources(IEnumerable<(string Source, decimal Amount)> sources, decimal totalRevenue, int limit = 5)
        {
            return sources
          .Where(s => s.Amount > 0)
    .OrderByDescending(s => s.Amount)
     .Take(limit)
         .Select(s => new SourceAmountDto
         {
             Source = s.Source,
             Amount = s.Amount,
             Percentage = totalRevenue > 0 ? Math.Round(s.Amount / totalRevenue * 100, 2) : 0
         })
   .ToList();
        }
    }
}
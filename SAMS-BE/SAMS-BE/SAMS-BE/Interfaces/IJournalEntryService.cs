using SAMS_BE.DTOs.JournalEntry;
using SAMS_BE.Models;

namespace SAMS_BE.Interfaces
{
    public interface IJournalEntryService
    {
     Task<JournalEntry> CreateJournalEntryFromReceiptAsync(Receipt receipt, Invoice invoice);
     Task<JournalEntry> CreateJournalEntryFromVoucherAsync(Voucher voucher);
        bool ValidateJournalEntry(JournalEntry entry);
     string GetAccountCodeFromPaymentMethod(PaymentMethod paymentMethod);
  
        // Reporting methods
    Task<(List<GeneralJournalDto> Items, int Total)> GetGeneralJournalAsync(JournalEntryQueryDto query);
        Task<GeneralJournalDto?> GetByIdAsync(Guid entryId);
        Task<IncomeStatementDto> GetIncomeStatementAsync(DateTime from, DateTime to);
        Task<FinancialDashboardDto> GetFinancialDashboardAsync(string period);
    }
}

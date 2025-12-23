using SAMS_BE.DTOs;
using SAMS_BE.Helpers;
using SAMS_BE.Interfaces;
using SAMS_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace SAMS_BE.Services
{
    public partial class JournalEntryService : IJournalEntryService
    {
        private readonly BuildingManagementContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JournalEntryService> _logger;

        public JournalEntryService(
               BuildingManagementContext context,
                    IConfiguration configuration,
               ILogger<JournalEntryService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<JournalEntry> CreateJournalEntryFromReceiptAsync(Receipt receipt, Invoice invoice)
        {
            try
            {
                _logger.LogInformation($"[JournalEntry] Starting creation for Receipt {receipt.ReceiptId}");

                // Check if accounting is enabled
                var enableAccounting = _configuration.GetValue<bool>("Features:EnableAccounting", false);
                _logger.LogInformation($"[JournalEntry] EnableAccounting config: {enableAccounting}");

                if (!enableAccounting)
                {
                    _logger.LogInformation("Accounting is disabled. Skipping journal entry creation.");
                    return null!;
                }

                _logger.LogInformation($"[JournalEntry] Receipt.Method is null: {receipt.Method == null}");

                // Load payment method if not already loaded
                if (receipt.Method == null)
                {
                    _logger.LogInformation($"[JournalEntry] Loading PaymentMethod {receipt.MethodId}");
                    receipt.Method = await _context.PaymentMethods
                .FirstOrDefaultAsync(pm => pm.PaymentMethodId == receipt.MethodId)
                  ?? throw new KeyNotFoundException($"Payment method {receipt.MethodId} not found");
                }

                _logger.LogInformation($"[JournalEntry] PaymentMethod loaded: {receipt.Method.Name} (Code: {receipt.Method.Code})");

                // Get entry date
                var entryDate = DateOnly.FromDateTime(receipt.ReceivedDate);

                // Get fiscal period (YYYY-MM format)
                var fiscalPeriod = JournalEntryHelper.GetFiscalPeriod(entryDate);

                // Get next sequence number for this month (for entry number generation)
                var sequenceNumber = await GetNextSequenceNumberAsync(entryDate.Year, entryDate.Month);

                // Generate entry number: JE-YYYY-MM-NNNN
                var entryNumber = JournalEntryHelper.GenerateEntryNumber(entryDate, sequenceNumber);

                _logger.LogInformation($"[JournalEntry] Generated entry number: {entryNumber}, FiscalPeriod: {fiscalPeriod}");

                // Try to find StaffProfile for CreatedBy (optional - for audit trail)
                Guid? staffCode = null;
                try
                {
                    var staffProfile = await _context.StaffProfiles
                        .FirstOrDefaultAsync(sp => sp.UserId == receipt.CreatedBy);
                    if (staffProfile != null)
                    {
                        staffCode = staffProfile.StaffCode;
                        _logger.LogInformation($"[JournalEntry] Found StaffProfile: {staffCode}");
                    }
                    else
                    {
                        _logger.LogWarning($"[JournalEntry] No StaffProfile found for User {receipt.CreatedBy}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"[JournalEntry] Could not lookup StaffProfile for User {receipt.CreatedBy}");
                }

                // Get account codes
                var debitAccount = GetAccountCodeFromPaymentMethod(receipt.Method);
                var creditAccount = JournalEntryHelper.ACCOUNT_REVENUE_SERVICE;

                _logger.LogInformation($"[JournalEntry] Account codes - Debit: {debitAccount}, Credit: {creditAccount}");

                // Create journal entry
                var journalEntry = new JournalEntry
                {
                    EntryId = Guid.NewGuid(),
                    EntryNumber = entryNumber,
                    EntryType = JournalEntryHelper.REF_TYPE_RECEIPT,
                    EntryDate = entryDate,
                    FiscalPeriod = fiscalPeriod,
                    ReferenceType = JournalEntryHelper.REF_TYPE_RECEIPT,
                    ReferenceId = receipt.ReceiptId,
                    Description = $"Thu ti?n t? {receipt.ReceiptNo} - Invoice {invoice.InvoiceNo} - C?n h? {invoice.Apartment?.Number ?? "N/A"}",
                    Status = JournalEntryHelper.STATUS_POSTED,
                    CreatedBy = staffCode,
                    PostedBy = staffCode,
                    PostedDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // Create journal entry lines
                var lines = new List<JournalEntryLine>
 {
   // Debit line (Cash/Bank)
        new JournalEntryLine
    {
       LineId = Guid.NewGuid(),
            EntryId = journalEntry.EntryId,
       LineNumber = 1,
           AccountCode = debitAccount,
      Description = $"Thu ti?n qua {receipt.Method.Name}",
        DebitAmount = receipt.AmountTotal,
    CreditAmount = 0,
ApartmentId = invoice.ApartmentId,
          CreatedAt = DateTime.UtcNow
    },
        // Credit line (Revenue)
   new JournalEntryLine
    {
            LineId = Guid.NewGuid(),
      EntryId = journalEntry.EntryId,
    LineNumber = 2,
        AccountCode = creditAccount,
        Description = $"Doanh thu d?ch v? - Invoice {invoice.InvoiceNo}",
      DebitAmount = 0,
    CreditAmount = receipt.AmountTotal,
        ApartmentId = invoice.ApartmentId,
          CreatedAt = DateTime.UtcNow
 }
     };

                journalEntry.JournalEntryLines = lines;

                // Validate before saving
                if (!ValidateJournalEntry(journalEntry))
                {
                    throw new InvalidOperationException("Journal entry is not balanced");
                }

                // Save to database
                await _context.JournalEntries.AddAsync(journalEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created journal entry {entryNumber} for receipt {receipt.ReceiptNo}");
                return journalEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating journal entry for receipt {receipt.ReceiptId}");
                throw;
            }
        }

        public async Task<JournalEntry> CreateJournalEntryFromVoucherAsync(Voucher voucher)
        {
            try
            {
                _logger.LogInformation($"[JournalEntry] Starting creation for Voucher {voucher.VoucherId}");

                // Check if accounting is enabled
                var enableAccounting = _configuration.GetValue<bool>("Features:EnableAccounting", false);
                _logger.LogInformation($"[JournalEntry] EnableAccounting config: {enableAccounting}");

                if (!enableAccounting)
                {
                    _logger.LogInformation("Accounting is disabled. Skipping journal entry creation.");
                    return null!;
                }

                // Load voucher items if not already loaded
                if (voucher.VoucherItems == null || !voucher.VoucherItems.Any())
                {
                    _logger.LogInformation($"[JournalEntry] Loading VoucherItems for Voucher {voucher.VoucherId}");
                    voucher = await _context.Vouchers
                      .Include(v => v.VoucherItems)
                 .ThenInclude(vi => vi.ServiceType)
                      .Include(v => v.VoucherItems)
                     .ThenInclude(vi => vi.Apartment)
                   .FirstOrDefaultAsync(v => v.VoucherId == voucher.VoucherId)
                                ?? throw new KeyNotFoundException($"Voucher {voucher.VoucherId} not found");
                }

                _logger.LogInformation($"[JournalEntry] Voucher loaded with {voucher.VoucherItems.Count} items");

                // Get entry date
                var entryDate = voucher.Date;

                // Get fiscal period (YYYY-MM format)
                var fiscalPeriod = JournalEntryHelper.GetFiscalPeriod(entryDate);

                // Get next sequence number for this month
                var sequenceNumber = await GetNextSequenceNumberAsync(entryDate.Year, entryDate.Month);

                // Generate entry number: JE-YYYY-MM-NNNN
                var entryNumber = JournalEntryHelper.GenerateEntryNumber(entryDate, sequenceNumber);

                _logger.LogInformation($"[JournalEntry] Generated entry number: {entryNumber}, FiscalPeriod: {fiscalPeriod}");

                // Try to find StaffProfile for CreatedBy (optional)
                Guid? staffCode = null;
                if (voucher.CreatedBy.HasValue)
                {
                    try
                    {
                        var staffProfile = await _context.StaffProfiles
                   .FirstOrDefaultAsync(sp => sp.UserId == voucher.CreatedBy.Value);
                        if (staffProfile != null)
                        {
                            staffCode = staffProfile.StaffCode;
                            _logger.LogInformation($"[JournalEntry] Found StaffProfile: {staffCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"[JournalEntry] Could not lookup StaffProfile for User {voucher.CreatedBy}");
                    }
                }

                // Determine account codes
                // Voucher (Payment) = Chi ti?n ra
                // Debit: Expense Account (Chi ph�)
                // Credit: Cash/Bank Account (Ti?n m?t/Ng�n h�ng)
                var creditAccount = JournalEntryHelper.ACCOUNT_CASH; // Default cash account
                var debitAccount = JournalEntryHelper.ACCOUNT_EXPENSE; // Default expense account

                _logger.LogInformation($"[JournalEntry] Account codes - Debit (Expense): {debitAccount}, Credit (Cash): {creditAccount}");

                // Create journal entry
                var journalEntry = new JournalEntry
                {
                    EntryId = Guid.NewGuid(),
                    EntryNumber = entryNumber,
                    EntryType = "PAYMENT", // Voucher type
                    EntryDate = entryDate,
                    FiscalPeriod = fiscalPeriod,
                    ReferenceType = "VOUCHER",
                    ReferenceId = voucher.VoucherId,
                    Description = $"Chi ti?n t? {voucher.VoucherNumber} - {voucher.Description}",
                    Status = JournalEntryHelper.STATUS_POSTED,
                    CreatedBy = staffCode,
                    PostedBy = staffCode,
                    PostedDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // Create journal entry lines from voucher items
                var lines = new List<JournalEntryLine>();
                var lineNumber = 1;

                foreach (var item in voucher.VoucherItems)
                {
                    // Debit line (Expense) - Chi ph� theo t?ng item
                    lines.Add(new JournalEntryLine
                    {
                        LineId = Guid.NewGuid(),
                        EntryId = journalEntry.EntryId,
                        LineNumber = lineNumber++,
                        AccountCode = debitAccount,
                        Description = item.Description ?? $"Chi ph� {item.ServiceType?.Name ?? "kh�c"}",
                        DebitAmount = item.Amount ?? 0,
                        CreditAmount = 0,
                        ApartmentId = item.ApartmentId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // Credit line (Cash/Bank) - T?ng ti?n chi ra
                lines.Add(new JournalEntryLine
                {
                    LineId = Guid.NewGuid(),
                    EntryId = journalEntry.EntryId,
                    LineNumber = lineNumber,
                    AccountCode = creditAccount,
                    Description = $"Chi ti?n m?t - {voucher.VoucherNumber}",
                    DebitAmount = 0,
                    CreditAmount = voucher.TotalAmount,
                    CreatedAt = DateTime.UtcNow
                });

                journalEntry.JournalEntryLines = lines;

                // Validate before saving
                if (!ValidateJournalEntry(journalEntry))
                {
                    throw new InvalidOperationException("Journal entry is not balanced");
                }

                // Save to database
                await _context.JournalEntries.AddAsync(journalEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created journal entry {entryNumber} for voucher {voucher.VoucherNumber}");
                return journalEntry;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating journal entry for voucher {voucher.VoucherId}");
                throw;
            }
        }

        public bool ValidateJournalEntry(JournalEntry entry)
        {
            if (entry.JournalEntryLines == null || !entry.JournalEntryLines.Any())
                return false;

            var totalDebit = entry.JournalEntryLines.Sum(l => l.DebitAmount ?? 0);
            var totalCredit = entry.JournalEntryLines.Sum(l => l.CreditAmount ?? 0);

            return totalDebit == totalCredit && totalDebit > 0;
        }

        public string GetAccountCodeFromPaymentMethod(PaymentMethod paymentMethod)
        {
            // Try to use code if available, otherwise use name
            var methodCode = !string.IsNullOrWhiteSpace(paymentMethod.Code)
        ? paymentMethod.Code
        : paymentMethod.Name;

            return JournalEntryHelper.MapPaymentMethodToAccount(methodCode);
        }

        private async Task<int> GetNextSequenceNumberAsync(int year, int month)
        {
            // Build fiscal period string (YYYY-MM)
            var fiscalPeriod = $"{year}-{month:D2}";

            // Count entries in this fiscal period
            var count = await _context.JournalEntries
          .Where(je => je.FiscalPeriod == fiscalPeriod)
         .CountAsync();

            return count + 1;
        }
    }
}

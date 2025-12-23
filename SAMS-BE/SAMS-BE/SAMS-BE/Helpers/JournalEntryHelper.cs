namespace SAMS_BE.Helpers
{
  /// <summary>
    /// Helper class for Journal Entry operations
    /// </summary>
    public static class JournalEntryHelper
    {
        // Journal Entry Statuses
     public const string STATUS_DRAFT = "DRAFT";
        public const string STATUS_POSTED = "POSTED";
        public const string STATUS_REVERSED = "REVERSED";

        // Reference Types
    public const string REF_TYPE_RECEIPT = "RECEIPT";
   public const string REF_TYPE_VOUCHER = "VOUCHER";
        public const string REF_TYPE_INVOICE = "INVOICE";

        // Account Codes (Chart of Accounts)
 public const string ACCOUNT_CASH = "1111"; // Tiền mặt
        public const string ACCOUNT_BANK = "1121";     // Tiền gửi ngân hàng
      public const string ACCOUNT_REVENUE_SERVICE = "5112";   // Doanh thu dịch vụ
   public const string ACCOUNT_EXPENSE = "6211";    // Chi phí chung
        public const string ACCOUNT_EXPENSE_REPAIR = "6271"; // Chi phí sửa chữa

        /// <summary>
    /// Generate Journal Entry Number in format: JE-YYYY-MM-NNNN
        /// Example: JE-2025-01-0001
        /// </summary>
  /// <param name="entryDate">Entry date</param>
        /// <param name="sequenceNumber">Sequence number for the month</param>
    /// <returns>Generated entry number</returns>
    public static string GenerateEntryNumber(DateOnly entryDate, int sequenceNumber)
        {
      return $"JE-{entryDate.Year}-{entryDate.Month:D2}-{sequenceNumber:D4}";
    }

     /// <summary>
        /// Generate fiscal period from date: YYYY-MM
        /// Example: 2025-01
        /// </summary>
    /// <param name="date">Date to extract fiscal period</param>
      /// <returns>Fiscal period string</returns>
        public static string GetFiscalPeriod(DateOnly date)
        {
    return $"{date.Year}-{date.Month:D2}";
        }

   /// <summary>
        /// Get fiscal period from DateTime
 /// </summary>
      public static string GetFiscalPeriod(DateTime date)
        {
      return GetFiscalPeriod(DateOnly.FromDateTime(date));
     }

/// <summary>
        /// Map payment method code to account code
        /// </summary>
        /// <param name="paymentMethodCode">Payment method code (CASH, VIETQR, etc.)</param>
   /// <returns>Account code</returns>
        public static string MapPaymentMethodToAccount(string? paymentMethodCode)
     {
     return paymentMethodCode?.ToUpperInvariant() switch
 {
  "CASH" => ACCOUNT_CASH,       // 1111 - Tiền mặt
"VIETQR" => ACCOUNT_BANK,     // 1121 - Ngân hàng (VietQR)
    "ONLINE_VIETQR" => ACCOUNT_BANK,  // 1121 - Ngân hàng
    "BANK_TRANSFER" => ACCOUNT_BANK,  // 1121 - Ngân hàng
     "MOMO" => ACCOUNT_BANK,// 1121 - Ngân hàng (e-wallet treated as bank)
     "ZALOPAY" => ACCOUNT_BANK,        // 1121 - Ngân hàng
    _ => ACCOUNT_CASH  // Default to cash
    };
  }
    }
}

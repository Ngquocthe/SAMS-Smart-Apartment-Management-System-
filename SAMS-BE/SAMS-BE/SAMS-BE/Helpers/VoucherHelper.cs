namespace SAMS_BE.Helpers
{
    /// <summary>
    /// Helper class cho Voucher (Phi?u chi) - Ch?a các constants và utility methods
    /// </summary>
    public static class VoucherHelper
 {
     #region Voucher Status Constants
        
        /// <summary>
 /// Nháp - Ch?ng t? ?ang so?n th?o, có th? ch?nh s?a/xóa
        /// ?? CH? dành cho voucher t?o th? công (manual) b?i k? toán
        /// Voucher t? Ticket/Maintenance s? b?t ??u ? PENDING
        /// </summary>
   public const string STATUS_DRAFT = "DRAFT";

        /// <summary>
        /// Ch? duy?t - Ch?ng t? ?ã g?i lên ch? k? toán tr??ng duy?t
   /// ? DEFAULT cho voucher t?o t? Ticket/Maintenance
        /// </summary>
   public const string STATUS_PENDING = "PENDING";

 /// <summary>
     /// ?ã duy?t - K? toán tr??ng ?ã duy?t
        /// ? T? ??ng t?o Journal Entry khi approve
    /// FINAL STATE - Không th? s?a/xóa/h?y sau khi approve
/// ?? KHÔNG th? chuy?n sang b?t k? status nào khác (k? c? CANCELLED)
   /// </summary>
        public const string STATUS_APPROVED = "APPROVED";

    /// <summary>
        /// ?ã h?y - Ch?ng t? không còn hi?u l?c
        /// </summary>
        public const string STATUS_CANCELLED = "CANCELLED";

        #endregion

        #region Voucher Type Constants

        /// <summary>
        /// Phi?u chi - Ghi nh?n các kho?n chi ti?n
    /// Ví d?: Chi l??ng nhân viên, chi s?a ch?a, chi mua v?t t?
        /// </summary>
        public const string TYPE_PAYMENT = "PAYMENT";

        #endregion

   #region Validation Methods

        /// <summary>
        /// Ki?m tra status có h?p l? không
        /// </summary>
        public static bool IsValidStatus(string status)
     {
            return status switch
            {
        STATUS_DRAFT => true,
        STATUS_PENDING => true,
        STATUS_APPROVED => true,
           STATUS_CANCELLED => true,
      _ => false
   };
 }

        /// <summary>
        /// Ki?m tra type có h?p l? không (ch? PAYMENT)
      /// </summary>
    public static bool IsValidType(string type)
  {
            return type == TYPE_PAYMENT;
  }

        /// <summary>
        /// Ki?m tra có th? chuy?n t? status này sang status khác không
        /// </summary>
        public static bool CanTransitionStatus(string fromStatus, string toStatus)
        {
            return (fromStatus, toStatus) switch
            {
         // T? DRAFT
     (STATUS_DRAFT, STATUS_PENDING) => true,
       (STATUS_DRAFT, STATUS_CANCELLED) => true,
   
        // T? PENDING
      (STATUS_PENDING, STATUS_APPROVED) => true,
         (STATUS_PENDING, STATUS_DRAFT) => true,
          (STATUS_PENDING, STATUS_CANCELLED) => true,
       
   // T? APPROVED - FINAL STATE, không th? chuy?n sang gì n?a
    // ?? APPROVED ?ã t?o Journal Entry, không th? cancel
    (STATUS_APPROVED, _) => false,
     
            // M?c ??nh không cho phép
                _ => false
            };
    }

      /// <summary>
        /// Ki?m tra status có th? s?a/xóa không
        /// </summary>
        public static bool CanEditOrDelete(string status)
      {
         return status == STATUS_DRAFT;
        }

        /// <summary>
    /// L?y tên hi?n th? c?a status
        /// </summary>
  public static string GetStatusDisplayName(string status)
        {
         return status switch
   {
    STATUS_DRAFT => "Nháp",
          STATUS_PENDING => "Ch? duy?t",
   STATUS_APPROVED => "?ã duy?t",
    STATUS_CANCELLED => "?ã h?y",
     _ => "Không xác ??nh"
    };
        }

     /// <summary>
/// L?y tên hi?n th? c?a type (ch? Phi?u chi)
        /// </summary>
        public static string GetTypeDisplayName(string type)
        {
 return type == TYPE_PAYMENT ? "Phi?u chi" : "Không xác ??nh";
        }

        #endregion

   #region Utility Methods

        /// <summary>
     /// Generate voucher number theo format
        /// Format: PC-{Year}-{SequenceNumber}
        /// Ví d?: PC-2025-001
        /// </summary>
 public static string GenerateVoucherNumber(int year, int sequenceNumber)
    {
            return $"PC-{year}-{sequenceNumber:D3}";
   }

        /// <summary>
        /// Ki?m tra Debit và Credit có cân b?ng không
     /// </summary>
        public static bool IsBalanced(decimal totalDebit, decimal totalCredit)
        {
      return Math.Abs(totalDebit - totalCredit) < 0.01m; // Cho phép sai s? 0.01
        }

        #endregion
    }
}

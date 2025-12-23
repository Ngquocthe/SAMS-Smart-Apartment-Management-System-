namespace SAMS_BE.Constants
{
    /// <summary>
    /// Các tr?ng thái c?a Voucher (Ch?ng t? k? toán)
    /// </summary>
    public static class VoucherStatus
    {
      /// <summary>
      /// Nháp - Ch?ng t? ?ang so?n th?o, có th? ch?nh s?a/xóa
        /// </summary>
        public const string DRAFT = "DRAFT";

        /// <summary>
    /// Ch? duy?t - Ch?ng t? ?ã g?i lên ch? k? toán tr??ng duy?t
        /// </summary>
        public const string PENDING = "PENDING";

        /// <summary>
        /// ?ã duy?t - K? toán tr??ng ?ã duy?t, ch? ghi s?
        /// </summary>
   public const string APPROVED = "APPROVED";

        /// <summary>
   /// ?ã ghi s? - Ch?ng t? ?ã ???c h?ch toán vào s? sách k? toán
        /// </summary>
        public const string POSTED = "POSTED";

    /// <summary>
  /// ?ã h?y - Ch?ng t? không còn hi?u l?c
     /// </summary>
        public const string CANCELLED = "CANCELLED";
    }

    /// <summary>
    /// Các lo?i Voucher (Ch?ng t? k? toán)
 /// </summary>
    public static class VoucherType
    {
        /// <summary>
        /// Phi?u thu - Ghi nh?n các kho?n thu ti?n
        /// Ví d?: Thu phí qu?n lý, thu ti?n ?i?n n??c t? c? dân
        /// </summary>
    public const string RECEIPT = "RECEIPT";

        /// <summary>
        /// Phi?u chi - Ghi nh?n các kho?n chi ti?n
        /// Ví d?: Chi l??ng nhân viên, chi s?a ch?a, chi mua v?t t?
 /// </summary>
        public const string PAYMENT = "PAYMENT";

        /// <summary>
  /// Ch?ng t? ghi s? - Ghi nh?n các nghi?p v? k? toán không qua ti?n m?t
        /// Ví d?: Phân b? chi phí, kh?u hao, ?i?u ch?nh s? sách
        /// </summary>
        public const string JOURNAL = "JOURNAL";
    }
}

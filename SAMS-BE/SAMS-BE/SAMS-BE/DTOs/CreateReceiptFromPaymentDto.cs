using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    /// <summary>
    /// DTO ?? t?o Receipt t? ??ng t? payment online (VietQR, MoMo, etc.)
    /// </summary>
    public class CreateReceiptFromPaymentDto
    {
  /// <summary>
        /// ID c?a Invoice c?n thanh toán
        /// </summary>
        [Required]
        public Guid InvoiceId { get; set; }

        /// <summary>
        /// S? ti?n ?ã thanh toán
        /// </summary>
  [Required]
[Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

   /// <summary>
        /// Mã ph??ng th?c thanh toán (VIETQR, MOMO, ZALOPAY, etc.)
        /// Default: VIETQR
        /// </summary>
        [StringLength(64)]
  public string? PaymentMethodCode { get; set; }

 /// <summary>
      /// Th?i gian thanh toán (t? payment gateway)
        /// Optional: N?u không cung c?p, l?y th?i gian hi?n t?i
        /// </summary>
   public DateTime? PaymentDate { get; set; }

        /// <summary>
        /// Ghi chú v? thanh toán
        /// </summary>
    [StringLength(1000)]
        public string? Note { get; set; }
    }
}

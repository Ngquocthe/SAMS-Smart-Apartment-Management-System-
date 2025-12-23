using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    /// <summary>
    /// Request DTO for creating a Voucher from a Ticket
    /// </summary>
    public class CreateVoucherRequest
    {
  [Required]
        public Guid TicketId { get; set; }

   [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity ph?i l?n h?n 0")]
        public decimal? Quantity { get; set; }

        [Range(0, double.MaxValue)]
      public decimal? UnitPrice { get; set; }

    [StringLength(1000)]
  public string? Note { get; set; }

        /// <summary>
        /// N?u t?o voucher t? hóa ??n, truy?n kèm InvoiceNo ?? set description cho voucher l?n ??u
        /// </summary>
        public string? InvoiceNo { get; set; }

        /// <summary>
        /// ServiceTypeId ?? gán vào VoucherItem
 /// </summary>
        public Guid? ServiceTypeId { get; set; }

        /// <summary>
        /// Ng??i th?c hi?n t?o voucher (?? ghi log vào ticket)
        /// </summary>
        public Guid? CreatedByUserId { get; set; }
    }
}

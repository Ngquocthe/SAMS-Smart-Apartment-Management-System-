using System.ComponentModel.DataAnnotations;
using SAMS_BE.Helpers;

namespace SAMS_BE.DTOs
{
    /// <summary>
    /// DTO for creating a Voucher (Phi?u chi)
    /// Type is always PAYMENT - no need to specify
    /// </summary>
    public class CreateVoucherDto
    {
        [StringLength(64)]
        public string? VoucherNumber { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        public string? CompanyInfo { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "TotalAmount must be greater than or equal to 0")]
        public decimal TotalAmount { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(32)]
        public string Status { get; set; } = "DRAFT";

        // Internal property for backward compatibility
        internal string Type => VoucherHelper.TYPE_PAYMENT;
    }
}
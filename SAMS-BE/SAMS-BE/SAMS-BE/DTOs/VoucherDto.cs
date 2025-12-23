using System.ComponentModel.DataAnnotations;

namespace SAMS_BE.DTOs
{
    /// <summary>
    /// DTO for Voucher response
    /// </summary>
    public class VoucherDto
    {
        public Guid VoucherId { get; set; }

        [StringLength(64)]
        public string VoucherNumber { get; set; } = string.Empty;

        [StringLength(32)]
        public string Type { get; set; } = string.Empty;

        public DateOnly Date { get; set; }

        /// <summary>
        /// Fiscal period in format YYYY/MM
        /// </summary>
        public string FiscalPeriod { get; set; } = string.Empty;

        [StringLength(500)]
        public string? CompanyInfo { get; set; }

        [StringLength(32)]
        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public Guid? TicketId { get; set; }

        public Guid? HistoryId { get; set; }

        public List<VoucherItemResponseDto> Items { get; set; } = new List<VoucherItemResponseDto>();
    }
}

using SAMS_BE.Models;

namespace SAMS_BE.DTOs
{
    public class VoucherListResponseDto
    {
        public IReadOnlyList<Voucher> Items { get; set; } = new List<Voucher>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
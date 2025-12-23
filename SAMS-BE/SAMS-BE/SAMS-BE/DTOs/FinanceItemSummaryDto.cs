namespace SAMS_BE.DTOs
{
    /// <summary>
    /// Summary DTO for finance items (Invoice/Voucher)
    /// </summary>
    public class FinanceItemSummaryDto
    {
        public Guid Id { get; set; }
        
        public string Number { get; set; } = string.Empty;
        
        public decimal Amount { get; set; }
    }
}

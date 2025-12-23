namespace SAMS_BE.DTOs
{
    public class InvoiceDetailResponseDto
    {
        public Guid InvoiceDetailId { get; set; }
        public Guid InvoiceId { get; set; }
        public Guid ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string? ServiceCode { get; set; }
        public string? ServiceUnit { get; set; }
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public decimal? VatRate { get; set; }
        public decimal? VatAmount { get; set; }
    }
}
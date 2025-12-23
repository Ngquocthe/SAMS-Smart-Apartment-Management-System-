namespace SAMS_BE.DTOs
{
    public class ReceiptListQueryDto
    {
        public Guid? InvoiceId { get; set; }
        public Guid? MethodId { get; set; }
        public string? Search { get; set; }
        public DateTime? ReceivedFrom { get; set; }
      public DateTime? ReceivedTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "ReceivedDate"; // ReceiptNo|ReceivedDate|AmountTotal
     public string SortDir { get; set; } = "desc";
    }
}

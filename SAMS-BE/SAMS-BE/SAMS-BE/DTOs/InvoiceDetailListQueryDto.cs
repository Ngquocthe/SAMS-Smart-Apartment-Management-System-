namespace SAMS_BE.DTOs
{
    public class InvoiceDetailListQueryDto
    {
    public Guid? InvoiceId { get; set; }
public Guid? ServiceId { get; set; }
   public string? Search { get; set; }
 public int Page { get; set; } = 1;
  public int PageSize { get; set; } = 20;
      public string SortBy { get; set; } = "ServiceName"; // ServiceName|Quantity|Amount
        public string SortDir { get; set; } = "asc";
    }
}

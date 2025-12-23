namespace SAMS_BE.DTOs
{
    public class VoucherListQueryDto
    {
        public Guid? ApartmentId { get; set; }
        public string? Type { get; set; }
      public string? Status { get; set; }
        public string? Search { get; set; }
        public DateOnly? DateFrom { get; set; }
  public DateOnly? DateTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "Date"; // VoucherNumber|Date|TotalAmount|Type
     public string SortDir { get; set; } = "desc";
    }
}

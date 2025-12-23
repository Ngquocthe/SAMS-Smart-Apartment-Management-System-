namespace SAMS_BE.DTOs
{
    public class VoucherItemListQueryDto
    {
        public Guid? VoucherId { get; set; }
     public Guid? ServiceTypeId { get; set; }
        public Guid? ApartmentId { get; set; }
        public Guid? TicketId { get; set; }
     public string? Search { get; set; }
        public int Page { get; set; } = 1;
   public int PageSize { get; set; } = 20;
  public string SortBy { get; set; } = "CreatedAt"; // ServiceTypeName|Amount|CreatedAt
  public string SortDir { get; set; } = "desc";
    }
}

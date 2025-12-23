namespace SAMS_BE.DTOs
{
    public class InvoiceListQueryDto
    {
        public Guid? ApartmentId { get; set; }
        public string? Status { get; set; }
        public string? Search { get; set; }
        public DateOnly? DueFrom { get; set; }
        public DateOnly? DueTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string SortBy { get; set; } = "DueDate"; // InvoiceNo|IssueDate|DueDate|TotalAmount
        public string SortDir { get; set; } = "desc";
    }
}

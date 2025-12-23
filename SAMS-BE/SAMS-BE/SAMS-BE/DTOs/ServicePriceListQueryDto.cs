namespace SAMS_BE.DTOs
{
    public class ServicePriceListQueryDto
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "EffectiveDate";
        public string? SortDir { get; set; } = "desc";
        public string? Q { get; set; }
        public string? Status { get; set; }
        public DateOnly? FromDate { get; set; }
        public DateOnly? ToDate { get; set; }
    }
}

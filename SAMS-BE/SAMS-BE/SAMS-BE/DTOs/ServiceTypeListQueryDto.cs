namespace SAMS_BE.DTOs
{
    public class ServiceTypeListQueryDto
    {
        public string? Q { get; set; }
        public Guid? CategoryId { get; set; }
        public bool? IsActive { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string? SortBy { get; set; } = "Name";
        public string? SortDir { get; set; } = "asc";
    }
}

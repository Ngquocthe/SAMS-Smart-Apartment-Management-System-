namespace SAMS_BE.DTOs
{
    public class ServiceTypeResponseDto
    {
        public Guid ServiceTypeId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? Unit { get; set; }
        public bool IsMandatory { get; set; }
        public bool IsRecurring { get; set; }
        public bool IsActive { get; set; }
        public bool IsDelete { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

namespace SAMS_BE.DTOs
{
    public class ServicePriceResponseDto
    {
        public Guid ServicePrices { get; set; }
        public Guid ServiceTypeId { get; set; }
        public string ServiceTypeCode { get; set; } = null!;
        public string ServiceTypeName { get; set; } = null!;
        public decimal UnitPrice { get; set; }
        public DateOnly EffectiveDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string Status { get; set; } = null!;
        public Guid? CreatedBy { get; set; }
        public string? CreatedByName { get; set; }
        public Guid? ApprovedBy { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

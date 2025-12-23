namespace SAMS_BE.DTOs.Response.Apartment
{
    public class ApartmentSummaryDto
    {
        public Guid ApartmentId { get; set; }
        public Guid FloorId { get; set; }
        public string Number { get; set; } = null!;
        public decimal? AreaM2 { get; set; }
        public int? Bedrooms { get; set; }
        public string Status { get; set; } = null!;
        public string? Type { get; set; }
        public string? Image { get; set; }
    }
}

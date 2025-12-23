namespace SAMS_BE.DTOs.Response.Resident
{
    public class ResidentApartmentDto
    {
        public Guid ResidentApartmentId { get; set; }
        public Guid ApartmentId { get; set; }
        public string RelationType { get; set; } = null!;
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsPrimary { get; set; }
        public DTOs.Response.Apartment.ApartmentSummaryDto? Apartment { get; set; }
    }
}

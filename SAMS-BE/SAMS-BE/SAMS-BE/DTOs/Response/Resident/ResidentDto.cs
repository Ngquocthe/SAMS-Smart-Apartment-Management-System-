namespace SAMS_BE.DTOs.Response.Resident
{
    public class ResidentDto
    {
        public Guid ResidentId { get; set; }
        public Guid? UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? IdNumber { get; set; }
        public DateOnly? Dob { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string Status { get; set; } = null!;
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<ResidentApartmentDto> Apartments { get; set; } = new();
    }
}

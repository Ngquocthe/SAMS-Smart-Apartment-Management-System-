namespace SAMS_BE.DTOs.Response
{
    public class LoginUserDto
    {
        public Guid UserId { get; set; }
        public string? Username { get; set; } = default!;
        public string? Email { get; set; } = default!;
        public string? Phone { get; set; }
        public string? FirstName { get; set; } = default!;
        public string? LastName { get; set; } = default!;
        public string? FullName { get; set; } = default!;
        public string? Dob { get; set; }
        public string? Address { get; set; }
        public string? AvatarUrl { get; set; }
        public string? CheckinPhotoUrl { get; set; }
    }
}

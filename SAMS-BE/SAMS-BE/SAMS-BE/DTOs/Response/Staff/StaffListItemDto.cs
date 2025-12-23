namespace SAMS_BE.DTOs.Response.Staff
{
    public class StaffListItemDto
    {
        public Guid StaffCode { get; init; }
        public Guid? UserId { get; init; }
        public string? FullName { get; init; }
        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public DateTime? HireDate { get; init; }
        public string? Role { get; init; }
        public DateTime? TerminationDate { get; init; }
        public bool IsActive => TerminationDate == null;
    }
}

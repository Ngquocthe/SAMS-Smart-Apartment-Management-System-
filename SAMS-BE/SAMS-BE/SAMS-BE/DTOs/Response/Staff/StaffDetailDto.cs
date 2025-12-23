namespace SAMS_BE.DTOs.Response.Staff
{
    public sealed class StaffDetailDto : StaffListItemDto
    {
        public string? Username { get; init; }
        public DateTime? Dob { get; init; }
        public string? Address { get; init; }
        public string? CurrentAddress { get; init; }
        public String BuildingId { get; set; }
        public string RoleId { get; init; } = string.Empty;
        public IReadOnlyList<string> AccessRoles { get; set; } = Array.Empty<string>();
        public decimal BaseSalary { get; init; }
        public string? Notes { get; init; }
        public string? EmergencyContactName { get; init; }
        public string? EmergencyContactPhone { get; init; }
        public string? EmergencyContactRelation { get; init; }
        public string? BankAccountNo { get; init; }
        public string? BankName { get; init; }
        public string? BankBranch { get; init; }
        public string? TaxCode { get; init; }
        public string? SocialInsuranceNo { get; init; }
        public string? AvatarUrl { get; init; }
        public string? CardPhotoUrl { get; init; }
    }
}

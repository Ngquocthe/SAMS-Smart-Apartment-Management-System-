namespace SAMS_BE.DTOs.Response.Staff
{
    public sealed class WorkRoleOptionDto
    {
        public Guid RoleId { get; init; }
        public string RoleKey { get; init; } = null!;
        public string RoleName { get; init; } = null!;
        public bool IsActive { get; init; }
    }
}

namespace SAMS_BE.DTOs
{
    public class VisibilityScopeDto
    {
        public string Value { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string[] AllowedRoles { get; set; } = null!;
    }
}

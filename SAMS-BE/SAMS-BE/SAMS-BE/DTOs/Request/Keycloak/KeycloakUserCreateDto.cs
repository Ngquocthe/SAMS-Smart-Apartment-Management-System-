namespace SAMS_BE.DTOs.Request.Keycloak
{
    public class KeycloakUserCreateDto
    {
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool Enabled { get; set; } = true;
    }
}

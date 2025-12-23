namespace SAMS_BE.DTOs.Response.Keycloak
{
    public record KeycloakRoleDto(
       string? Id,
       string Name,
       string? Description,
       bool? Composite = null,
       bool? ClientRole = null,
       string? ContainerId = null
   );
}

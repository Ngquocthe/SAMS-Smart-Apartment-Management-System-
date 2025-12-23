using SAMS_BE.DTOs.Request.Keycloak;
using SAMS_BE.DTOs.Response.Keycloak;

namespace SAMS_BE.Interfaces.IService.Keycloak
{
    public interface IKeycloakRoleService
    {
        Task<IEnumerable<KeycloakRoleDto>> GetClientRolesAsync(string? clientId = null, CancellationToken ct = default);
        Task<IEnumerable<KeycloakRoleDto>> GetUserClientRolesAsync(
         string userId,
         string? clientId = null,
         CancellationToken ct = default);

        Task<string?> FindUserIdByUsernameAsync(string username, CancellationToken ct = default);

        Task<(string KeycloakUserId, string TempPassword)> CreateUserAsync(KeycloakUserCreateDto dto, CancellationToken ct);
        Task AssignClientRolesToUserAsync(string keycloakUserId, string clientId, IEnumerable<string> roleNames, CancellationToken ct);

        Task RemoveClientRolesFromUserAsync(string keycloakUserId, string clientId, IEnumerable<string> roleNames, CancellationToken ct);
        Task DeleteUserAsync(string keycloakUserId, CancellationToken ct);

        Task<string?> FindUserIdByEmailAsync(string email, CancellationToken ct);
    }
}

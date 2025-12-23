using SAMS_BE.Config.Backchannel;
using SAMS_BE.DTOs.Request.Keycloak;
using SAMS_BE.DTOs.Response.Keycloak;
using SAMS_BE.Interfaces.IService.Keycloak;
using SAMS_BE.Utils;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SAMS_BE.Services.Keycloak
{
    public class KeycloakRoleService : IKeycloakRoleService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _cfg;
        private readonly IKeycloakTokenService _tokenSvc;
        private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public KeycloakRoleService(IHttpClientFactory httpClientFactory, IConfiguration cfg, IKeycloakTokenService tokenSvc)
        {
            _httpClientFactory = httpClientFactory;
            _cfg = cfg;
            _tokenSvc = tokenSvc;
        }

        private (string hostBase, string realm) ParseAuthority()
        {
            // ví dụ Authority: https://auth.fhub.club/realms/NOAH
            var authority = _cfg["Keycloak:Authority"] ?? throw new InvalidOperationException("Keycloak:Authority is required.");
            var uri = new Uri(authority);
            var realm = uri.Segments.Last().TrimEnd('/');
            var hostBase = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
            return (hostBase, realm);
        }

        private async Task<HttpClient> CreateAuthedClientAsync(CancellationToken ct)
        {
            var token = await _tokenSvc.GetClientTokenAsync(null);
            var http = _httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return http;
        }

        public async Task<IEnumerable<KeycloakRoleDto>> GetClientRolesAsync(
      string? clientId = null,
      CancellationToken ct = default)
        {
            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            clientId ??= _cfg["Keycloak:ClientId"]
                ?? throw new InvalidOperationException("Keycloak:ClientId is required.");

            var http = await CreateAuthedClientAsync(ct);

            var findUrl = $"{baseAdmin}/clients?clientId={Uri.EscapeDataString(clientId)}";
            using var r1 = await http.GetAsync(findUrl, ct);
            var b1 = await r1.Content.ReadAsStringAsync(ct);
            if (!r1.IsSuccessStatusCode)
                throw new InvalidOperationException($"Find client failed {(int)r1.StatusCode}: {b1}");

            using var doc = JsonDocument.Parse(b1);
            var client = doc.RootElement.EnumerateArray().FirstOrDefault(e =>
                string.Equals(e.GetProperty("clientId").GetString(), clientId, StringComparison.Ordinal));

            if (client.ValueKind == JsonValueKind.Undefined)
                return Enumerable.Empty<KeycloakRoleDto>();

            var clientUuid = client.GetProperty("id").GetString()
                ?? throw new InvalidOperationException("Client UUID not found.");

            var rolesUrl = $"{baseAdmin}/clients/{clientUuid}/roles";
            using var r2 = await http.GetAsync(rolesUrl, ct);
            var b2 = await r2.Content.ReadAsStringAsync(ct);
            if (!r2.IsSuccessStatusCode)
                throw new InvalidOperationException($"Client roles failed {(int)r2.StatusCode}: {b2}");

            var excludedRoles = new HashSet<string>(
                new[] { "global_admin", "resident" },
                StringComparer.OrdinalIgnoreCase
            );

            var roles = JsonSerializer
                .Deserialize<IEnumerable<KeycloakRoleDto>>(b2, _json)
                ?? Enumerable.Empty<KeycloakRoleDto>();

            return roles
                .Where(r => !excludedRoles.Contains(r.Name))
                .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string?> ResolveClientUuidAsync(HttpClient http, string baseAdmin, string clientId, CancellationToken ct)
        {
            var findUrl = $"{baseAdmin}/clients?clientId={Uri.EscapeDataString(clientId)}";
            using var r = await http.GetAsync(findUrl, ct);
            var body = await r.Content.ReadAsStringAsync(ct);
            if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"Find client failed {(int)r.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var client = doc.RootElement.EnumerateArray().FirstOrDefault(e =>
                string.Equals(e.GetProperty("clientId").GetString(), clientId, StringComparison.Ordinal));
            return client.ValueKind == JsonValueKind.Undefined ? null : client.GetProperty("id").GetString();
        }

        public async Task<IEnumerable<KeycloakRoleDto>> GetUserClientRolesAsync(string userId, string? clientId = null, CancellationToken ct = default)
        {
            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            clientId ??= _cfg["Keycloak:ClientId"] ?? throw new InvalidOperationException("Keycloak:ClientId is required.");

            var http = await CreateAuthedClientAsync(ct);
            var clientUuid = await ResolveClientUuidAsync(http, baseAdmin, clientId, ct)
                ?? throw new InvalidOperationException($"Client '{clientId}' not found.");

            // roles đã gán trực tiếp cho user ở client này
            var url = $"{baseAdmin}/users/{Uri.EscapeDataString(userId)}/role-mappings/clients/{clientUuid}";
            using var r = await http.GetAsync(url, ct);
            var body = await r.Content.ReadAsStringAsync(ct);
            if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"User client roles failed {(int)r.StatusCode}: {body}");

            var roles = JsonSerializer.Deserialize<IEnumerable<KeycloakRoleDto>>(body, _json) ?? Enumerable.Empty<KeycloakRoleDto>();
            return roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<string?> FindUserIdByUsernameAsync(string username, CancellationToken ct = default)
        {
            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            var http = await CreateAuthedClientAsync(ct);

            // exact=true để tránh trả về list nhiều user
            var url = $"{baseAdmin}/users?username={Uri.EscapeDataString(username)}&exact=true";
            using var r = await http.GetAsync(url, ct);
            var body = await r.Content.ReadAsStringAsync(ct);
            if (!r.IsSuccessStatusCode) throw new InvalidOperationException($"Find user failed {(int)r.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var first = doc.RootElement.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Undefined ? null : first.GetProperty("id").GetString();
        }

        public async Task<(string KeycloakUserId, string TempPassword)> CreateUserAsync(KeycloakUserCreateDto dto, CancellationToken ct)
        {
            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            var http = await CreateAuthedClientAsync(ct);

            var url = $"{baseAdmin}/users";

            var tempPassword = AuthGuards.GenerateSecurePassword(12);

            var payload = new
            {
                username = dto.Username,
                email = dto.Email,
                firstName = dto.FirstName,
                lastName = dto.LastName,
                enabled = dto.Enabled,
                emailVerified = false,
                credentials = new[]
                {
                    new
                    {
                        type      = "password",
                        value     = tempPassword,
                        temporary = true  
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload, _json);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await http.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (response.StatusCode != HttpStatusCode.Created &&
                response.StatusCode != HttpStatusCode.NoContent)
            {
                throw new InvalidOperationException(
                    $"Create Keycloak user failed {(int)response.StatusCode}: {body}"
                );
            }

            string? keycloakUserId = null;
            var location = response.Headers.Location?.ToString();
            if (!string.IsNullOrWhiteSpace(location))
            {
                keycloakUserId = location.TrimEnd('/').Split('/').LastOrDefault();
            }

            if (string.IsNullOrWhiteSpace(keycloakUserId))
            {
                keycloakUserId = await FindUserIdByUsernameAsync(dto.Username, ct);
            }

            if (string.IsNullOrWhiteSpace(keycloakUserId))
            {
                throw new InvalidOperationException(
                    $"Create Keycloak user succeeded but cannot resolve user id for username '{dto.Username}'."
                );
            }

            return (keycloakUserId, tempPassword);
        }


        public async Task AssignClientRolesToUserAsync(string keycloakUserId, string clientId, IEnumerable<string> roleNames, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keycloakUserId))
                throw new ArgumentException("keycloakUserId is required.", nameof(keycloakUserId));

            var names = roleNames?
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (names.Length == 0)
            {
                return;
            }

            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            clientId ??= _cfg["Keycloak:ClientId"] ??
                         throw new InvalidOperationException("Keycloak:ClientId is required.");

            var http = await CreateAuthedClientAsync(ct);

            // 1. Resolve client UUID
            var clientUuid = await ResolveClientUuidAsync(http, baseAdmin, clientId, ct)
                ?? throw new InvalidOperationException($"Client '{clientId}' not found.");

            // 2. Lấy toàn bộ roles của client để map name -> full role object
            var rolesUrl = $"{baseAdmin}/clients/{clientUuid}/roles";
            using var rRoles = await http.GetAsync(rolesUrl, ct);
            var rolesBody = await rRoles.Content.ReadAsStringAsync(ct);
            if (!rRoles.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Get client roles failed {(int)rRoles.StatusCode}: {rolesBody}"
                );
            }

            var allRoles = JsonSerializer.Deserialize<IEnumerable<KeycloakRoleDto>>(rolesBody, _json)
                          ?? Enumerable.Empty<KeycloakRoleDto>();

            var selectedRoles = allRoles
                .Where(r => names.Contains(r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (selectedRoles.Length == 0)
            {
                throw new InvalidOperationException(
                    $"None of the role names [{string.Join(", ", names)}] exist on client '{clientId}'."
                );
            }

            var assignUrl =
                $"{baseAdmin}/users/{Uri.EscapeDataString(keycloakUserId)}/role-mappings/clients/{clientUuid}";

            var json = JsonSerializer.Serialize(selectedRoles, _json);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var rAssign = await http.PostAsync(assignUrl, content, ct);
            var assignBody = await rAssign.Content.ReadAsStringAsync(ct);

            if (!rAssign.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Assign client roles failed {(int)rAssign.StatusCode}: {assignBody}"
                );
            }
        }


        public async Task DeleteUserAsync(string keycloakUserId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keycloakUserId))
                return;

            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            var http = await CreateAuthedClientAsync(ct);

            var url = $"{baseAdmin}/users/{Uri.EscapeDataString(keycloakUserId)}";
            using var response = await http.DeleteAsync(url, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode &&
                response.StatusCode != HttpStatusCode.NotFound)
            {
                throw new InvalidOperationException(
                    $"Delete Keycloak user failed {(int)response.StatusCode}: {body}"
                );
            }
        }

        public async Task<string?> FindUserIdByEmailAsync(string email, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            var http = await CreateAuthedClientAsync(ct);

            var url = $"{baseAdmin}/users?email={Uri.EscapeDataString(email)}&exact=true";
            using var r = await http.GetAsync(url, ct);
            var body = await r.Content.ReadAsStringAsync(ct);
            if (!r.IsSuccessStatusCode)
                throw new InvalidOperationException($"Find user by email failed {(int)r.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var first = doc.RootElement.EnumerateArray().FirstOrDefault();
            return first.ValueKind == JsonValueKind.Undefined ? null : first.GetProperty("id").GetString();
        }

        public async Task RemoveClientRolesFromUserAsync(
    string keycloakUserId,
    string clientId,
    IEnumerable<string> roleNames,
    CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(keycloakUserId))
                throw new ArgumentException("keycloakUserId is required.", nameof(keycloakUserId));

            var names = roleNames?
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();

            if (names.Length == 0)
            {
                return; 
            }

            var (hostBase, realm) = ParseAuthority();
            var baseAdmin = $"{hostBase}/admin/realms/{realm}";
            clientId ??= _cfg["Keycloak:ClientId"]
                         ?? throw new InvalidOperationException("Keycloak:ClientId is required.");

            var http = await CreateAuthedClientAsync(ct);

            var clientUuid = await ResolveClientUuidAsync(http, baseAdmin, clientId, ct)
                ?? throw new InvalidOperationException($"Client '{clientId}' not found.");

            var rolesUrl = $"{baseAdmin}/clients/{clientUuid}/roles";
            using var rRoles = await http.GetAsync(rolesUrl, ct);
            var rolesBody = await rRoles.Content.ReadAsStringAsync(ct);

            if (!rRoles.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Get client roles failed {(int)rRoles.StatusCode}: {rolesBody}"
                );
            }

            var allRoles = JsonSerializer.Deserialize<IEnumerable<KeycloakRoleDto>>(rolesBody, _json)
                           ?? Enumerable.Empty<KeycloakRoleDto>();

            var rolesToRemove = allRoles
                .Where(r => names.Contains(r.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (rolesToRemove.Length == 0)
            {
                return;
            }

            var deleteUrl =
                $"{baseAdmin}/users/{Uri.EscapeDataString(keycloakUserId)}/role-mappings/clients/{clientUuid}";

            var json = JsonSerializer.Serialize(rolesToRemove, _json);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Delete, deleteUrl)
            {
                Content = content
            };

            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Remove client roles failed {(int)response.StatusCode}: {body}"
                );
            }
        }

    }
}

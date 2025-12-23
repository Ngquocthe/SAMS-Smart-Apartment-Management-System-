using Microsoft.Extensions.Caching.Memory;
using SAMS_BE.Config.Backchannel;
using System.Net.Http.Headers;
using System.Text.Json;

namespace SAMS_BE.Security
{
    public interface IKeycloakResourceService
    {
        Task<HashSet<string>> GetProtectedPermissionsAsync();
    }

    public class KeycloakResourceService : IKeycloakResourceService
    {
        private readonly IHttpClientFactory _http;
        private readonly IKeycloakTokenService _tokenService;
        private readonly IConfiguration _cfg;
        private readonly IMemoryCache _cache;

        private const string CACHE_KEY = "kc:protected-permissions";
        private const string CLIENT_UUID_CACHE_KEY = "kc:client-uuid";

        public KeycloakResourceService(
            IHttpClientFactory http,
            IKeycloakTokenService tokenService,
            IConfiguration cfg,
            IMemoryCache cache)
        {
            _http = http;
            _tokenService = tokenService;
            _cfg = cfg;
            _cache = cache;
        }

        public async Task<HashSet<string>> GetProtectedPermissionsAsync()
        {
            Console.WriteLine(">>> ENTER GetProtectedPermissionsAsync");

            if (_cache.TryGetValue(CACHE_KEY, out HashSet<string> cached) && cached != null)
            {
                Console.WriteLine($">>> CACHE HIT: count = {cached.Count}");
                if (cached.Count > 0)
                    return cached;

                Console.WriteLine(">>> CACHE EMPTY -> force reload");
            }

            try
            {
                var adminBase = _cfg["Keycloak:AdminBaseUrl"]!.TrimEnd('/');
                var realm = _cfg["Keycloak:Realm"];
                var clientId = _cfg["Keycloak:Audience"];

                Console.WriteLine($">>> CONFIG authority={adminBase}, realm={realm}, clientId={clientId}");

                if (string.IsNullOrWhiteSpace(adminBase)
                    || string.IsNullOrWhiteSpace(realm)
                    || string.IsNullOrWhiteSpace(clientId))
                {
                    Console.WriteLine(">>> CONFIG INVALID -> ALLOW ALL");
                    return new HashSet<string>();
                }

                var clientUuid = await GetClientUuidAsync(adminBase, realm, clientId);
                Console.WriteLine($">>> CLIENT UUID = {clientUuid}");

                var resourceEndpoint =
                    $"{adminBase}/admin/realms/{realm}/clients/{clientUuid}/authz/resource-server/resource";

                Console.WriteLine($">>> CALL KC: {resourceEndpoint}");

                var token = await _tokenService.GetClientTokenAsync();
                var http = _http.CreateClient(); 
                http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);

                var resp = await http.GetAsync(resourceEndpoint);

                Console.WriteLine($">>> KC STATUS = {(int)resp.StatusCode}");

                if (!resp.IsSuccessStatusCode)
                {
                    Console.WriteLine(">>> KC NOT SUCCESS -> ALLOW ALL");
                    return new HashSet<string>();
                }

                var json = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($">>> KC BODY = {json}");

                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Array)
                {
                    Console.WriteLine(">>> KC RESPONSE NOT ARRAY -> ALLOW ALL");
                    return new HashSet<string>();
                }

                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var res in doc.RootElement.EnumerateArray())
                {
                    if (!res.TryGetProperty("name", out var nameEl)) continue;
                    var resource = nameEl.GetString()?.ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(resource)) continue;

                    if (!res.TryGetProperty("scopes", out var scopes)
                        || scopes.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var s in scopes.EnumerateArray())
                    {
                        if (!s.TryGetProperty("name", out var scopeEl)) continue;
                        var scope = scopeEl.GetString()?.ToUpperInvariant();
                        if (string.IsNullOrWhiteSpace(scope)) continue;

                        var key = $"{resource}:{scope}";
                        set.Add(key);

                        Console.WriteLine($">>> ADD PERMISSION {key}");
                    }
                }

                if (set.Count > 0)
                {
                    _cache.Set(CACHE_KEY, set, TimeSpan.FromMinutes(3));
                    Console.WriteLine($">>> CACHE SET: {set.Count} permissions");
                }
                else
                {
                    Console.WriteLine(">>> NO PERMISSION FOUND -> NOT CACHED");
                }

                return set;
            }
            catch (Exception ex)
            {
                Console.WriteLine(">>> KC ERROR:");
                Console.WriteLine(ex);
                return new HashSet<string>();
            }
        }

        // ==========================================================
        // GET CLIENT UUID
        // ==========================================================
        private async Task<string> GetClientUuidAsync(string authority, string realm, string clientId)
        {
            if (_cache.TryGetValue(CLIENT_UUID_CACHE_KEY, out string cached) && !string.IsNullOrWhiteSpace(cached))
            {
                Console.WriteLine($">>> CLIENT UUID CACHE HIT: {cached}");
                return cached;
            }

            Console.WriteLine(">>> LOAD CLIENT UUID FROM KC");

            var token = await _tokenService.GetClientTokenAsync();
            var client = _http.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var url = $"{authority}/admin/realms/{realm}/clients?clientId={clientId}";
            Console.WriteLine($">>> CALL KC: {url}");

            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                throw new Exception("Cannot load client UUID");

            var json = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($">>> CLIENT LIST = {json}");

            using var doc = JsonDocument.Parse(json);

            var uuid = doc.RootElement
                .EnumerateArray()
                .FirstOrDefault()
                .GetProperty("id")
                .GetString();

            if (string.IsNullOrWhiteSpace(uuid))
                throw new Exception("Client UUID not found");

            _cache.Set(CLIENT_UUID_CACHE_KEY, uuid, TimeSpan.FromHours(1));
            return uuid;
        }
    }
}

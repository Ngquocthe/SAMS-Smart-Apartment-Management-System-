using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;

namespace SAMS_BE.Config.Backchannel
{
    public interface IKeycloakTokenService
    {
        Task<string> GetClientTokenAsync(string? scope = null);
    }

    public class KeycloakTokenService : IKeycloakTokenService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _cfg;
        private readonly IMemoryCache _cache;

        // OIDC discovery manager (thread-safe, auto-refresh)
        private readonly ConfigurationManager<OpenIdConnectConfiguration> _oidcManager;

        public KeycloakTokenService(IHttpClientFactory httpClientFactory, IConfiguration cfg, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cfg = cfg;
            _cache = cache;

            var authority = _cfg["Keycloak:Authority"]?.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(authority))
                throw new InvalidOperationException("Keycloak:Authority is required.");

            var metadataAddress = authority.EndsWith("/.well-known/openid-configuration", StringComparison.OrdinalIgnoreCase)
                ? authority
                : $"{authority}/.well-known/openid-configuration";

            _oidcManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever()
            );
        }

        public async Task<string> GetClientTokenAsync(string? scope = null)
        {
            var key = $"kc_cc_token::{scope ?? "default"}";
            if (_cache.TryGetValue(key, out string cached)) return cached;

            var clientId = _cfg["Keycloak:ClientId"] ?? throw new InvalidOperationException("Keycloak:ClientId is required.");
            var clientSecret = _cfg["Keycloak:ClientSecret"] ?? throw new InvalidOperationException("Keycloak:ClientSecret is required.");

            var configuredTokenEndpoint = _cfg["Keycloak:TokenEndpoint"];
            string tokenEndpoint;
            if (!string.IsNullOrWhiteSpace(configuredTokenEndpoint))
            {
                tokenEndpoint = configuredTokenEndpoint!;
            }
            else
            {
                var oidcConfig = await _oidcManager.GetConfigurationAsync(default);
                tokenEndpoint = oidcConfig.TokenEndpoint
                    ?? throw new InvalidOperationException("Cannot resolve token endpoint from OIDC discovery.");
            }

            var http = _httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint);

            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var pairs = new List<KeyValuePair<string, string>> {
            new("grant_type", "client_credentials"),
            new("client_id", clientId),
            new("client_secret", clientSecret)
        };
            if (!string.IsNullOrWhiteSpace(scope))
                pairs.Add(new("scope", scope));

            req.Content = new FormUrlEncodedContent(pairs);

            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            var payload = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                // Trả lỗi gọn + body để dễ debug (log tuỳ môi trường)
                throw new InvalidOperationException($"Token request failed ({(int)resp.StatusCode}): {payload}");
            }

            var token = ParseAccessToken(payload, out var expiresIn);
            if (string.IsNullOrEmpty(token))
                throw new InvalidOperationException("No access_token in token response.");

            // Cache trước khi hết hạn (buffer 30s)
            var lifetime = TimeSpan.FromSeconds(Math.Max(expiresIn - 30, 30));
            _cache.Set(key, token, lifetime);

            return token;
        }

        private static string ParseAccessToken(string json, out int expiresIn)
        {
            expiresIn = 60;
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var accessToken = root.TryGetProperty("access_token", out var at)
                    ? at.GetString()
                    : null;

                if (root.TryGetProperty("expires_in", out var ei) && ei.TryGetInt32(out var sec))
                    expiresIn = sec;

                return accessToken ?? string.Empty;
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Invalid token JSON payload.", ex);
            }
        }
    }
}

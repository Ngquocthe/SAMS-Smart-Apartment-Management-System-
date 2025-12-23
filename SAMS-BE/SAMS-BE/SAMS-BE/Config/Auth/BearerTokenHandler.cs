using SAMS_BE.Config.Backchannel;
using System.Net.Http.Headers;

namespace SAMS_BE.Config.Auth
{
    public class BearerTokenHandler : DelegatingHandler
    {
        private readonly IKeycloakTokenService _tokenService;

        public BearerTokenHandler(IKeycloakTokenService tokenService)
        {
            _tokenService = tokenService;
            InnerHandler = new HttpClientHandler();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = await _tokenService.GetClientTokenAsync();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, ct);
        }
    }
}

namespace SAMS_BE.Config.Downstream
{
    public interface ICoreServiceClient
    {
        Task<string> GetHealthAsync();
    }

    public class CoreServiceClient : ICoreServiceClient
    {
        private readonly HttpClient _http;

        public CoreServiceClient(HttpClient http) => _http = http;

        public async Task<string> GetHealthAsync()
        {
            var resp = await _http.GetAsync("/health");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }
    }
}

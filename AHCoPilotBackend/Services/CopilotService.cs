using AHCoPilotBackend.Configuration;
using AHCoPilotBackend.Interfaces;
using AHCoPilotBackend.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace AHCoPilotBackend.Services
{
    public class CopilotService : ICopilotService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<CopilotService> _logger;


        public CopilotService(IHttpClientFactory httpClientFactory, IOptions<AppSettings> appSettings, ILogger<CopilotService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<Stream> GetCompletionsAsync(string token, Payload payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("GithubCopilot");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.PostAsJsonAsync(_appSettings.Value.CopilotApiUrl, payload);
                response.EnsureSuccessStatusCode();

                return await response.Content.ReadAsStreamAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Copilot API");
                throw;
            }
        }
    }
}

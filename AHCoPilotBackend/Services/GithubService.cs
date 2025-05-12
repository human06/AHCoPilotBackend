using AHCoPilotBackend.Configuration;
using AHCoPilotBackend.Interfaces;
using Microsoft.Extensions.Options;
using Octokit;

namespace AHCoPilotBackend.Services
{
    public class GithubService : IGithubService
    {
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ILogger<GithubService> _logger;

        public GithubService(IOptions<AppSettings> appSettings, ILogger<GithubService> logger)
        {
            _appSettings = appSettings;
            _logger = logger;
        }

        public async Task<User> GetUserAsync(string token)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue(_appSettings.Value.AppName))
                {
                    Credentials = new Credentials(token)
                };
                return await client.User.Current();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving GitHub user");
                throw;
            }
        }
    }
}

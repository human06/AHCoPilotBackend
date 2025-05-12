using Octokit;

namespace AHCoPilotBackend.Interfaces
{
    public interface IGithubService
    {
        Task<User> GetUserAsync(string token);
    }
}

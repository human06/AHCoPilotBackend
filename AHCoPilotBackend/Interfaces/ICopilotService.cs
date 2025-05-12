using AHCoPilotBackend.Models;

namespace AHCoPilotBackend.Interfaces
{
    public interface ICopilotService
    {
        Task<Stream> GetCompletionsAsync(string token, Payload payload);
    }
}

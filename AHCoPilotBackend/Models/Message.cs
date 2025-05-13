namespace AHCoPilotBackend.Models
{
    public class Message
    {
        public required string Role { get; set; }
        public required string Content { get; set; }
        public List<File> copilot_references { get; set; }

    }
}

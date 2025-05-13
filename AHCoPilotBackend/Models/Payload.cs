namespace AHCoPilotBackend.Models
{
    public class Payload
    {
        public bool Stream { get; set; }

        public List<Message> Messages { get; set; } = new List<Message>();
    }
}

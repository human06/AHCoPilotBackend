using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AHCoPilotBackend.Models
{
    public class File
    {
        public string type { get; set; }
        public Data data { get; set; }
        public string id { get; set; }
        public bool is_implicit { get; set; }
        public Metadata metadata { get; set; }
    }

    public class Metadata
    {
        public string display_name { get; set; }
        public string display_icon { get; set; }
        public string display_url { get; set; }
    }

    public class Data
    {
        public string content { get; set; }
        public string language { get; set; }
    }
}

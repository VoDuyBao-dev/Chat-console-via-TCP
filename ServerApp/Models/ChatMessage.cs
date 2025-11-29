namespace ServerApp.Models
{
    public class ChatMessage
    {
        public string FromDisplay { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
}

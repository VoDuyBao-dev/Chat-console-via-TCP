namespace ServerApp.Models;

public class GroupMessageDto
{
    public string DisplayName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}
using System.Collections.Concurrent;

namespace ServerApp.Models
{
    public class ChatGroup
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public int CreatorId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Danh sách thành viên hiện đang online trong nhóm (chỉ lưu trong RAM)
        public ConcurrentBag<User> OnlineMembers { get; set; } = new();
    }
}
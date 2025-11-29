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
        public List<User> OnlineMembers { get; set; } = new();
        private readonly object _onlineLock = new object();

        public void AddMember(User user)
        {
            lock (_onlineLock)
            {
                if (!OnlineMembers.Any(u => u.UserId == user.UserId))
                    OnlineMembers.Add(user);
            }
        }

        public void RemoveMember(int userId)
        {
            lock (_onlineLock)
            {
                OnlineMembers.RemoveAll(u => u.UserId == userId);
            }
        }

        public int OnlineCount => OnlineMembers.Count;
    }
}
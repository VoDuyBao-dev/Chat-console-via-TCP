using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using ServerApp.Models;

namespace ServerApp.Services
{
    public class ChatCommandService
    {
        private readonly ConcurrentDictionary<string, User> _clients;
        private readonly ConcurrentDictionary<int, ChatGroup> _groups = new();
        private readonly DatabaseService _db;
        private readonly Func<string, User?, Task> _broadcast;

        public ChatCommandService(
            ConcurrentDictionary<string, User> clients,
            DatabaseService db,
            Func<string, User?, Task> broadcast)
        {
            _clients = clients;
            _db = db;
            _broadcast = broadcast;
        }

        // PUBLIC MESSAGE
        public async Task HandlePublicAsync(User sender, string msg)
        {
            await _broadcast($"[{sender.DisplayName}]: {msg}", sender);
        }

        // PRIVATE MESSAGE
        public async Task HandlePrivateMessageAsync(User sender, string targetName, string msg)
        {
            // Validate: empty message or empty target
            if (string.IsNullOrWhiteSpace(targetName) || string.IsNullOrWhiteSpace(msg))
            {
                await sender.Writer.WriteLineAsync("[SERVER] Usage: PM|<DisplayName>|<message>");
                return;
            }

            // Prevent sending PM to yourself
            if (string.Equals(sender.DisplayName, targetName, StringComparison.OrdinalIgnoreCase))
            {
                await sender.Writer.WriteLineAsync("[SERVER] You cannot send a private message to yourself.");
                return;
            }

            // Find target by DisplayName
            User? target = _clients.Values
                .FirstOrDefault(u => string.Equals(u.DisplayName, targetName, StringComparison.OrdinalIgnoreCase));


            if (target != null)
            {
                // Send private message to the target
                await target.Writer.WriteLineAsync($"[PRIVATE from {sender.DisplayName}]: {msg}");

                // Confirm to the sender
                await sender.Writer.WriteLineAsync($"[PRIVATE to {target.DisplayName}]: {msg}");

                // Save to database if both are real users (not Guest)
                if (sender.UserId != 0 && target.UserId != 0)
                {
                    await _db.SaveMessageAsync(sender.UserId, target.UserId, msg);
                }
            }
            else
            {
                await sender.Writer.WriteLineAsync($"[SERVER] User '{targetName}' not found.");
            }
        }

        // Chat group
        // Tạo nhóm
        public async Task HandleCreateGroupAsync(User sender, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName) || groupName.Length > 50)
            {
                await sender.Writer.WriteLineAsync("[SERVER] Group name invalid (1-50 character).");
                return;
            }

            // Lưu vào db
            int groupId = await _db.CreateGroupAsync(sender.UserId, groupName);
            if (groupId <= 0)
            {
                await sender.Writer.WriteLineAsync("[SERVER] Unable to create group. Please try again!");
                return;
            }

            var group = new ChatGroup
            {
                GroupId = groupId,
                GroupName = groupName,
                CreatorId = sender.UserId
            };
            group.OnlineMembers.Add(sender);
            _groups[groupId] = group;

            await sender.Writer.WriteLineAsync(
            $"[SERVER] Group created successfully! Name: '{groupName}' | ID: {groupId} (Use this ID to invite/send messages)");
        }

        // Mời người vào nhóm 
        public async Task HandleInviteToGroupAsync(User sender, string targetName, int groupId)
        {
            if (!_groups.TryGetValue(groupId, out var group))
            {
                await sender.Writer.WriteLineAsync("[SERVER] Group not existed.");
                return;
            }

            var target = _clients.Values.FirstOrDefault(u => 
                string.Equals(u.DisplayName, targetName, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                await sender.Writer.WriteLineAsync($"[SERVER] User not found '{targetName}'.");
                return;
            }

            // Kiểm tra quyền (chỉ admin hoặc creator mới được mời)
            bool isAdmin = group.CreatorId == sender.UserId || 
                        await _db.IsGroupAdminAsync(groupId, sender.UserId);

            if (!isAdmin)
            {
                await sender.Writer.WriteLineAsync("[SERVER] You don't have permission to invite members.");
                return;
            }

            // Thêm vào DB
            bool added = await _db.AddUserToGroupAsync(groupId, target.UserId);
            if (!added)
            {
                await sender.Writer.WriteLineAsync("[SERVER] Cannot add member (they may already be in the group).");
                return;
            }

            // Nếu user đang online → thêm vào danh sách RAM
            if (group.OnlineMembers.All(m => m.UserId != target.UserId))
            {
                group.OnlineMembers.Add(target);
            }

            await sender.Writer.WriteLineAsync(
                $"[SERVER] {target.DisplayName} has been invited to the group '{group.GroupName}' (ID: {groupId})");

            await target.Writer.WriteLineAsync(
                $"[GROUP] You have been invited to the group '{group.GroupName}' (ID: {groupId})");

        }


        

        // USERS
        public async Task HandleUsersAsync(User sender)
        {
            var onlineUser = _clients.Values.Where(u => u.UserId != sender.UserId);
            var sb = new StringBuilder("===== Online Users =====\n");

            foreach (var u in onlineUser)
                sb.AppendLine($" - {u.DisplayName}");

            sb.Append("=========================");
            await sender.Writer.WriteLineAsync(sb.ToString());
        }

        // HELP
        public async Task HandleHelpAsync(User sender)
        {
    
            var help = """
        ===== Chat Commands =====
        msg|text                 - Send a public message
        /pm|<user>|<msg>         - Send a private message
        /users                   - Show list of online users
        /mygroups                - Show the groups you are in

        ---- Group Chat ----
        /creategroup|<group name> - Create a new group (you become admin)
        /invite|<user>|<group ID> - Invite a user to the group (admin only)
        /g|<group ID>|<msg>       - Send a message to a group (e.g., /g|5|Hello everyone)
        /mygroups                  - Show your groups + number of online members

        /help                     - Show this help menu
        exit                      - Leave the chat room
        ==========================
        """;
            await sender.Writer.WriteLineAsync(help);
        }
    }
}

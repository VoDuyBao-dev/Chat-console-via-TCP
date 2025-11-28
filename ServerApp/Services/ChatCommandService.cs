using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using ServerApp.Models;

namespace ServerApp.Services
{
    public class ChatCommandService
    {
        private readonly ConcurrentDictionary<string, User> _clients;
        private readonly ConcurrentDictionary<int, ChatGroup> _groups;
        private readonly DatabaseService _db;
        private readonly Func<string, User?, Task> _broadcast;

        public ChatCommandService(
            ConcurrentDictionary<string, User> clients,
            ConcurrentDictionary<int, ChatGroup> groups,
            DatabaseService db,
            Func<string, User?, Task> broadcast)
        {
            _clients = clients;
            _groups = groups;
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
                await sender.Writer.WriteLineAsync("[SERVER] Unable to create group or group name existed. Please try again!");
                return;
            }

            // Thêm user vừa tạo nhóm vào thành viên của nhóm đó
            await _db.AddUserToGroupAsync(groupId, sender.UserId, "admin"); // creator là admin

            var group = new ChatGroup
            {
                GroupId = groupId,
                GroupName = groupName,
                CreatorId = sender.UserId,
                OnlineMembers = new() { sender }
            };
           
            _groups[groupId] = group;

            await sender.Writer.WriteLineAsync(
            $"[SERVER] Group created successfully! Name: '{groupName}' | ID: {groupId} (Use this ID to invite/send messages)");
        }

        // Mời người vào nhóm  chỉ admin mới được mời
        public async Task HandleInviteToGroupAsync(User sender, string targetName, int groupId)
        {
            // 1. Check if group exists in memory
            if (!_groups.TryGetValue(groupId, out var group))
            {
                await sender.Writer.WriteLineAsync($"[SERVER] Group with ID {groupId} does not exist or server just restarted.");
                return;
            }

            // 2. Find target user (online only)
            var target = _clients.Values.FirstOrDefault(u =>
                string.Equals(u.DisplayName, targetName, StringComparison.OrdinalIgnoreCase));

            if (target == null)
            {
                await sender.Writer.WriteLineAsync($"[SERVER] User '{targetName}' not found or currently offline.");
                return;
            }

            if (target.UserId == sender.UserId)
            {
                await sender.Writer.WriteLineAsync("[SERVER] You cannot invite yourself to the group.");
                return;
            }

            // 3. Permission check: only admins can invite
            if (!await _db.IsGroupAdminAsync(groupId, sender.UserId))
            {
                await sender.Writer.WriteLineAsync("[SERVER] Only group admins can invite new members.");
                return;
            }

            // 4. Add user to group in database (default role = member)
            if (!await _db.AddUserToGroupAsync(groupId, target.UserId, role: "member"))
            {
                await sender.Writer.WriteLineAsync($"[SERVER] {target.DisplayName} is already in the group.");
                return;
            }

            // 5. Add to online members list if not already present
            if (!group.OnlineMembers.Any(m => m.UserId == target.UserId))
            {
                group.OnlineMembers.Add(target);
            }

            // 6. Send confirmation messages
            string groupInfo = $"'{group.GroupName}' (ID: {groupId})";

            await sender.Writer.WriteLineAsync(
                $"[SERVER] Successfully invited {target.DisplayName} to group {groupInfo}");

            await target.Writer.WriteLineAsync(
                $"[GROUP] You have been invited to group {groupInfo}\n" +
                $"       Type '/g {groupId} <message>' to chat in this group!");
        }
         // === Gửi tin nhắn nhóm ===
        public async Task HandleGroupMessageAsync(User sender, int groupId, string msg)
        {
            

            if (!_groups.TryGetValue(groupId, out var group))
            {
                await sender.Writer.WriteLineAsync("[SERVER] The group doesn't exist.");
                return;
            }

            // Kiểm tra sender có trong nhóm không
           
            bool isMember = await _db.IsUserInGroupAsync(groupId, sender.UserId);
            if (!isMember)
            {
                await sender.Writer.WriteLineAsync("[SERVER] You are not a member of this group.");
                return;
            }

            //Cập nhật OnlineMembers nếu chưa có (đề phòng restart server)
            if (group.OnlineMembers.All(u => u.UserId != sender.UserId))
            {
                group.OnlineMembers.Add(sender);
            }

            string message = $"[GROUP {group.GroupName}] {sender.DisplayName}: {msg}";

            // Gửi cho tất cả thành viên online trong nhóm
            var tasks = group.OnlineMembers.Select(member =>
                member.Writer.WriteLineAsync(message));

            await Task.WhenAll(tasks);

            // Lưu vào lịch sử chat nhóm vào db
            await _db.SaveGroupMessageAsync(groupId, sender.UserId, msg);
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

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
            if (string.Equals(sender.DisplayName, targetName, StringComparison.Ordinal))
            {
                await sender.Writer.WriteLineAsync("[SERVER] You cannot send a private message to yourself.");
                return;
            }

            // Find target by DisplayName
            User? target = _clients.Values
                .FirstOrDefault(u => string.Equals(u.DisplayName, targetName, StringComparison.Ordinal));


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
        // === GỬI TIN NHẮN NHÓM - CHỈ KHI ĐANG Ở CHẾ ĐỘ NHÓM (CurrentGroupId) ===
        public async Task HandleGroupMessageAsync(User sender, string msg)
        {
            // Bắt buộc phải đang ở trong một nhóm (đã dùng /join)
            if (!sender.CurrentGroupId.HasValue)
            {
                await sender.Writer.WriteLineAsync("[SERVER] You are not in any group. Use '/join <id>' to enter a group.");
                return;
            }

            int groupId = sender.CurrentGroupId.Value;

            if (!_groups.TryGetValue(groupId, out var group))
            {
                // Nhóm bị xóa? Tự động thoát chế độ nhóm
                sender.CurrentGroupId = null;
                await sender.Writer.WriteLineAsync("[SERVER] Group no longer exists. You have been returned to public chat.");
                return;
            }

            // Đảm bảo người gửi vẫn là thành viên (an toàn)
            if (!await _db.IsUserInGroupAsync(groupId, sender.UserId))
            {
                sender.CurrentGroupId = null;
                await sender.Writer.WriteLineAsync("[SERVER] You are no longer a member of this group. Returned to public chat.");
                return;
            }

            // Cập nhật OnlineMembers (đề phòng restart)
            if (group.OnlineMembers.All(u => u.UserId != sender.UserId))
            {
                group.AddMember(sender);
            }

            // Tạo tin nhắn đẹp
            string message = $"[GROUP {group.GroupName}] {sender.DisplayName}: {msg}";

            // Gửi cho tất cả thành viên online trong nhóm
            var sendTasks = group.OnlineMembers
                .Where(m => m.UserId != sender.UserId) // không gửi lại cho người gửi (người gửi thấy ngay)
                .Select(m => m.Writer.WriteLineAsync(message));

            await Task.WhenAll(sendTasks);

            // Gửi lại cho chính người gửi (để thấy tin của mình)
            await sender.Writer.WriteLineAsync(message);

            // Lưu vào database
            await _db.SaveGroupMessageAsync(groupId, sender.UserId, msg);
        }

        // Liệt kê nhóm của user tương ứng
        public async Task HandleMyGroupsAsync(User sender)
        {
            var myGroups = await _db.GetUserGroupsAsync(sender.UserId);

            if (!myGroups.Any())
            {
                await sender.Writer.WriteLineAsync("[SERVER] You haven't joined any groups yet.");
                return;
            }

            var sb = new StringBuilder("===== Your Groups =====\n");
            foreach (var g in myGroups)
            {
                int onlineCount = _groups.TryGetValue(g.GroupId, out var cg) 
                    ? cg.OnlineMembers.Count 
                    : 0;

                sb.AppendLine($"{g.GroupName} ({onlineCount} online)");
            }
            sb.Append("==========================");
            await sender.Writer.WriteLineAsync(sb.ToString());
        }

        // === VÀO NHÓM CHAT ===
        public async Task HandleJoinGroupAsync(User sender, int groupId)
        {
            if (!_groups.TryGetValue(groupId, out var group))
            {
                await sender.Writer.WriteLineAsync($"[SERVER] Group ID {groupId} does not exist.");
                return;
            }

            if (!await _db.IsUserInGroupAsync(groupId, sender.UserId))
            {
                await sender.Writer.WriteLineAsync("[SERVER] You are not a member of this group!");
                return;
            }

            sender.CurrentGroupId = groupId;

            await sender.Writer.WriteLineAsync($"");
            await sender.Writer.WriteLineAsync($"=== YOU HAVE ENTERED GROUP CHAT ===");
            await sender.Writer.WriteLineAsync($"[GROUP] {group.GroupName} (ID: {groupId})");
            await sender.Writer.WriteLineAsync($"Type '/leave' to exit group chat");
            await sender.Writer.WriteLineAsync($"====================================");

            // HIỂN THỊ LỊCH SỬ TIN NHẮN CŨ (10 tin gần nhất)
            var history = await _db.GetGroupMessageHistoryAsync(groupId, limit: 20);
            foreach (var msg in history)
            {
                await sender.Writer.WriteLineAsync($"[HISTORY] {msg.DisplayName}: {msg.Content}");
            }
            await sender.Writer.WriteLineAsync($"--- Now chatting in group ---");
        }

        // === THOÁT KHỎI NHÓM ===
        public async Task HandleLeaveGroupAsync(User sender)
        {
            if (sender.CurrentGroupId == null)
            {
                await sender.Writer.WriteLineAsync("[SERVER] You are not in any group chat.");
                return;
            }

            var oldGroupId = sender.CurrentGroupId.Value;
            sender.CurrentGroupId = null;

            if (_groups.TryGetValue(oldGroupId, out var group))
            {
                await sender.Writer.WriteLineAsync($"");
                await sender.Writer.WriteLineAsync($"[SERVER] You have left group '{group.GroupName}'");
                await sender.Writer.WriteLineAsync($"[SERVER] Back to public chat room.");
            }
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
            ================== CHAT COMMANDS ==================

            msg|<text>                 Send a message.
                                    - If you are in Public Chat → sends to public.
                                    - If you are in a Group Chat → sends to that group.             
                            
            /pm|<user>|<message>       Send a private message
            /users                     Show online users
            /mygroups                  List your groups + online members

            ----- Group Chat Commands -----
            /creategroup|<name>        Create a new group (you become the admin)
            /invite|<user>|<id>        Invite a user to a group (admin only)
            /join|<id>                 Join a group
            /leave                     Leave current group

            /help                      Show this help menu
            exit                       Exit the chat room

            =====================================================
            """;


            await sender.Writer.WriteLineAsync(help);
        }
    }
}

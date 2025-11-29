using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using ServerApp.Models;
using ServerApp.Utilities;


namespace ServerApp.Services
{
    public class ChatCommandService
    {
        private readonly ConcurrentDictionary<string, User> _clients;
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
            if (string.IsNullOrWhiteSpace(msg))
                return;
            await _broadcast($"[{sender.DisplayName}]: {msg}", sender);
        }

        // vào phòng chat riêng
        public async Task<bool> EnterPrivateChatAsync(User sender, string targetDisplayName)
        {
            if (string.IsNullOrWhiteSpace(targetDisplayName))
            {
                await sender.Writer.WriteLineAsync("[privatechat_error] Invalid target name.");
                return false;
            }

            // Không cho chat với chính mình
            if (string.Equals(sender.DisplayName, targetDisplayName, StringComparison.OrdinalIgnoreCase))
            {
                await sender.Writer.WriteLineAsync("[privatechat_error] You cannot chat with yourself.");
                return false;
            }

            // 1) Tìm online
            var targetOnline = _clients.Values
                .FirstOrDefault(u =>
                    string.Equals(u.DisplayName, targetDisplayName, StringComparison.OrdinalIgnoreCase));

            // 2) Tìm trong database (cả offline)
            var dbUser = await _db.GetUserByDisplayNameAsync(targetDisplayName);

            if (dbUser == null)
            {
                await sender.Writer.WriteLineAsync($"[privatechat_error] User '{targetDisplayName}' not found.");
                return false;
            }

            // Luôn set ID và Name cho private chat
            sender.PrivateChatTargetId = dbUser.Value.UserId;
            sender.PrivateChatTargetName = dbUser.Value.DisplayName;
            sender.InPrivateChat = true;

            if (targetOnline == null)
            {
                await sender.Writer.WriteLineAsync($"[SERVER] User '{targetDisplayName}' is not online.");
                sender.InPrivateChat = false;
                sender.PrivateChatTargetId = null;
                sender.PrivateChatTargetName = null;
                return false;
            }


            // ===== CASE 2: TARGET ONLINE =====
            sender.PrivateChatTarget = targetOnline;

            ConsoleLogger.Info($"session.DisplayName          = {targetOnline.DisplayName}");
            await sender.Writer.WriteLineAsync(
                $"[SERVER] Entered private chat with {targetOnline.DisplayName}. Type /exitpm to leave.");

            // Load lịch sử
            var history = await _db.GetChatHistoryAsync(sender.UserId, dbUser.Value.UserId);

            foreach (var h in history)
            {
                if (h.FromDisplay == sender.DisplayName)
                    await sender.Writer.WriteLineAsync($"[PM TO {dbUser.Value.DisplayName}]: {h.Message}");
                else
                    await sender.Writer.WriteLineAsync($"[PM FROM {h.FromDisplay}]: {h.Message}");
            }
            await sender.Writer.WriteLineAsync("[privatechat_ok]");
            return true;
        }


        // Gủi tin nhắn trong phòng chat riêng
        public async Task SendPrivateChatMessageAsync(User sender, string msg)
        {
            if (!sender.InPrivateChat || sender.PrivateChatTargetName == null)
            {


                await sender.Writer.WriteLineAsync("[SERVER] You are not in a private chat oke.");
                return;
            }

            if (string.IsNullOrWhiteSpace(msg))
            {
                await sender.Writer.WriteLineAsync("[SERVER] Cannot send empty message.");
                return;
            }

            var target = sender.PrivateChatTarget;
            ConsoleLogger.Info($"session.DisplayName     hehe      = {target.PrivateChatTargetName}");
            ConsoleLogger.Info($"session.DisplayName     hehe      = {target.PrivateChatTarget}");
            ConsoleLogger.Info($"session.DisplayName     hehe      = {target.Writer}");

            // Nếu target online — gửi tin thật
            if (target.Writer != null)
            {
                await target.Writer.WriteLineAsync($"[PM from {sender.DisplayName}]: {msg}");
                await sender.Writer.WriteLineAsync($"[PM to {target.DisplayName}]: {msg}");
            }
            else
            {
                // target offline — chỉ báo tin đã gửi
                await sender.Writer.WriteLineAsync($"[PM to {target.DisplayName}]: {msg} (offline)");
            }

            // Lưu database
            if (sender.UserId != 0 && target.UserId != 0)
                await _db.SaveMessageAsync(sender.UserId, target.UserId, msg);
        }


        // thoát phòng 
        public async Task ExitPrivateChatAsync(User sender)
        {
            if (!sender.InPrivateChat)
            {
                await sender.Writer.WriteLineAsync("[SERVER] Exit: You are not in a private chat.");
                return;
            }

            var other = sender.PrivateChatTarget!;
            sender.PrivateChatTarget = null;
            sender.InPrivateChat = false;


            await sender.Writer.WriteLineAsync($"[SERVER] You left the private chat with {other.DisplayName}.");
            await sender.Writer.WriteLineAsync("[SERVER] Returned to the public room.");
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
                /pm|<user>               - Send a private message
                /users                   - Show list of online users
                /help                    - Show this help menu
                exit                     - Leave the chat room
                """;
            await sender.Writer.WriteLineAsync(help);
        }
    }
}

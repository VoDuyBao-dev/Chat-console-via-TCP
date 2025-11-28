using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using ServerApp.Models;

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
            await _broadcast($"[{sender.Username}]: {msg}", sender);
        }

        // PRIVATE MESSAGE
        public async Task HandlePrivateMessageAsync(User sender, string targetName, string msg)
        {
            // Validate: empty message or empty target
            if (string.IsNullOrWhiteSpace(targetName) || string.IsNullOrWhiteSpace(msg))
            {
                await sender.Writer.WriteLineAsync("[SERVER] Usage: PM|<username>|<message>");
                return;
            }

            // Prevent sending PM to yourself
            if (string.Equals(sender.Username, targetName, StringComparison.OrdinalIgnoreCase))
            {
                await sender.Writer.WriteLineAsync("[SERVER] You cannot send a private message to yourself.");
                return;
            }

            // Check if target exists
            if (_clients.TryGetValue(targetName, out User? target))
            {
                // Send private message to the target
                await target.Writer.WriteLineAsync($"[PRIVATE from {sender.Username}]: {msg}");

                // Confirm to the sender
                await sender.Writer.WriteLineAsync($"[PRIVATE to {targetName}]: {msg}");

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
                msg|text                - Send a public message
                /pm|<user>|<msg>         - Send a private message
                /users                   - Show list of online users
                /help                    - Show this help menu
                exit                    - Leave the chat room
                """;
            await sender.Writer.WriteLineAsync(help);
        }
    }
}

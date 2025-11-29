// ServerApp/Services/ClientConnectionService.cs
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using Common;
using ServerApp.Models;
using ServerApp.Utilities;

namespace ServerApp.Services
{
    public class ClientConnectionService
    {
        private readonly ConcurrentDictionary<string, User> _clients = new();
        private readonly DatabaseService _db;

        private readonly AuthService _auth;
        private readonly ChatCommandService _commands;

        public ClientConnectionService()
        {
            string conn = ConfigService.GetConnectionString();
            _db = new DatabaseService(conn);

            _auth = new AuthService(_db, _clients);

            _commands = new ChatCommandService(
                _clients,
                _db,
                (msg, exclude) => BroadcastAsync(msg, exclude));
        }
        public void HandleNewClient(TcpClient tcpClient)
        {

            _ = ProcessClientAsync(tcpClient);
        }

        private async Task ProcessClientAsync(TcpClient tcpClient)
        {
            var user = new User(tcpClient);
            string? endpoint = tcpClient.Client.RemoteEndPoint?.ToString();

            try
            {
                // AUTH PHASE
                while (true)
                {
                    string? raw = await NetworkHelper.SafeReadLineAsync(user.Stream);
                    if (raw == null) return;

                    var msg = MessageParser.Parse(raw);

                    AuthResult result = msg.Command switch
                    {
                        Protocol.REGISTER => await _auth.HandleRegisterAsync(user, msg.Args),
                        Protocol.LOGIN => await _auth.HandleLoginAsync(user, msg.Args),
                        _ => AuthResult.Fail("[SERVER] Invalid authentication command.")
                    };

                    if (!result.Success)
                    {
                        await user.Writer.WriteLineAsync(result.ErrorMessage);
                        continue;
                    }

                    // Login/Register succeeded
                    await user.Writer.WriteLineAsync(result.SuccessToken);
                    break;
                }


                // JOIN ROOM
                ConsoleLogger.Join($"{user.DisplayName} ({endpoint}) has joined");
                await BroadcastAsync($"[SERVER] {user.DisplayName} has joined the public room! (Online: {_clients.Count})");
                // await BroadcastUserListAsync();
                await _commands.HandleHelpAsync(user);

                // await user.Writer.WriteLineAsync($"Welcome {user.DisplayName}! '/help' for commands.");
                await user.Writer.FlushAsync();

                // ===== VÒNG LẶP CHAT =====
                while (true)
                {
                    string? raw = await NetworkHelper.SafeReadLineAsync(user.Stream);
                    if (raw == null) break;

                    var msg = MessageParser.Parse(raw);

                    switch (msg.Command)
                    {
                        // PUBLIC MESSAGE
                        case Protocol.MSG:
                            // phòng chung
                            if (msg.Args.Length > 0)
                                await _commands.HandlePublicAsync(user, msg.Args[0]);
                            break;
                        // Vào phòng chat riêng 
                        case Protocol.ENTER_PM:
                            if (msg.Args.Length < 1)
                            {
                                await user.Writer.WriteLineAsync("[SERVER] Usage: ENTERPM|<DisplayName>");
                                break;
                            }
                            bool ok = await _commands.EnterPrivateChatAsync(user, msg.Args[0]);
                            if (ok)
                                ConsoleLogger.Info($"{user.DisplayName} entered private chat with {msg.Args[0]}");
                            // else await _commands.ExitPrivateChatAsync(user);
                            break;
                        // Gửi tin nhắn riêng khi đã vào phòng
                        case Protocol.PRIVMSG:
                            if (user.InPrivateChat)
                                await _commands.SendPrivateChatMessageAsync(user, msg.Args[0]);
                            break;

                        // Thoát phòng chat riêng
                        case Protocol.EXIT_PM:
                            if (user.InPrivateChat)
                                await _commands.ExitPrivateChatAsync(user);
                            break;

                        // USER LIST
                        case Protocol.USERS:
                            await _commands.HandleUsersAsync(user);
                            break;

                        // HELP
                        case Protocol.HELP:
                            await _commands.HandleHelpAsync(user);
                            break;

                        // EXIT
                        case Protocol.EXIT:
                            return; // luôn thoát client
                        default:
                            await user.Writer.WriteLineAsync("[SERVER] Unknown command.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.Error($"Client error {endpoint}: {ex.Message}");
            }
            finally
            {
                _clients.TryRemove(user.Username, out _);


                if (user.UserId != 0)
                    await _db.SetOfflineAsync(user.UserId);

                ConsoleLogger.Info($"{user.DisplayName} ({endpoint}) has disconnected");
                await BroadcastAsync($"[SERVER] {user.DisplayName} has left the room!");
                await BroadcastUserListAsync();

                tcpClient.Close();
            }
        }



        //tạo tên gợi ý
        private string GenerateSuggestedName(string original)
        {

            for (int i = 2; i < 100; i++)
            {
                string candidate = $"{original}_{i}";
                if (!_clients.ContainsKey(candidate))
                    return candidate;
            }

            // Nếu vẫn trùng, dùng Guest số ngẫu nhiên
            var random = new Random();
            while (true)
            {
                string guestName = $"Guest{random.Next(1000, 9999)}";
                if (!_clients.ContainsKey(guestName))
                    return guestName;
            }
        }




        private async Task BroadcastAsync(string message, User? exclude = null)
        {
            var tasks = new List<Task>();

            foreach (var client in _clients.Values)
            {
                if (client == exclude) continue;

                try
                {
                    tasks.Add(client.Writer.WriteLineAsync(message));
                }
                catch
                {

                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }

        private async Task BroadcastUserListAsync()
        {
            var sb = new StringBuilder("========== Online ==========\n");
            foreach (var u in _clients.Values)
            {
                sb.AppendLine($" - {u.DisplayName} ({u.Client.Client.RemoteEndPoint})");
            }
            sb.Append("============================");
            await BroadcastAsync(sb.ToString());
        }
    }
}
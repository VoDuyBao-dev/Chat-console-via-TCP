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

        public ClientConnectionService()
        {
            string conn = ConfigService.GetConnectionString();
            _db = new DatabaseService(conn);
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
                // ===== GIAI ĐOẠN AUTH (REGISTER/LOGIN/GUEST) =====
                bool inChat = false;

                while (!inChat)
                {
                    string? line = await NetworkHelper.SafeReadLineAsync(user.Stream);
                    if (line == null)
                    {
                        // client đóng trước khi auth
                        return;
                    }

                    // REGISTER|u|hash|display
                    if (line.StartsWith("REGISTER|", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('|');
                        if (parts.Length < 4)
                        {
                            await user.Writer.WriteLineAsync("[SERVER] REGISTER không hợp lệ.");
                            continue;
                        }

                        string regUser = parts[1];
                        string regPassHash = parts[2];
                        string display = parts[3];

                        if (await _db.UsernameOrDisplayExistsAsync(regUser, display))
                        {
                            await user.Writer.WriteLineAsync("[SERVER] Username hoặc DisplayName đã tồn tại!");
                            continue;
                        }

                        await _db.RegisterAsync(regUser, regPassHash, display);
                        await user.Writer.WriteLineAsync("[SERVER] Đăng ký thành công! Bạn có thể dùng menu client để đăng nhập.");
                        continue; // vẫn ở giai đoạn auth
                    }

                    // LOGIN|u|hash
                    if (line.StartsWith("LOGIN|", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = line.Split('|');
                        if (parts.Length < 3)
                        {
                            await user.Writer.WriteLineAsync("[SERVER] LOGIN không hợp lệ.");
                            continue;
                        }

                        string loginUser = parts[1];
                        string loginHash = parts[2];

                        var record = await _db.LoginAsync(loginUser, loginHash);
                        if (record == null)
                        {
                            await user.Writer.WriteLineAsync("[SERVER] Sai username hoặc password.");
                            continue;
                        }

                        user.UserId = record.Value.UserId;
                        user.Username = loginUser;
                        user.DisplayName = record.Value.DisplayName;

                        await _db.SetOnlineAsync(user.UserId);
                        _clients.TryAdd(user.Username, user);

                        await user.Writer.WriteLineAsync("===LOGIN_SUCCESS===");
                        inChat = true;
                        break;
                    }

                    // GUEST
                    if (line.Equals("GUEST", StringComparison.OrdinalIgnoreCase))
                    {
                        user.UserId = 0;
                        user.Username = "Guest_" + Random.Shared.Next(1000, 9999);

                        _clients.TryAdd(user.Username, user);

                        await user.Writer.WriteLineAsync("===LOGIN_SUCCESS===");
                        inChat = true;
                        break;
                    }

                    await user.Writer.WriteLineAsync("[SERVER] Lệnh không hợp lệ ở giai đoạn đăng nhập.");
                }

                // ===== ĐÃ VÀO PHÒNG CHAT =====

                ConsoleLogger.Join($"{user.Username} ({endpoint}) has joined");
                await BroadcastAsync($"[SERVER] {user.Username} has joined the room! (Online: {_clients.Count})");
                await BroadcastUserListAsync();

                await user.Writer.WriteLineAsync(
                    $"Welcome {user.Username}! Dùng /pm <username> <message> để chat riêng (user thật), 'exit' để thoát.");
                await user.Writer.FlushAsync();

                // ===== VÒNG LẶP CHAT =====
                string? message;
                while ((message = await NetworkHelper.SafeReadLineAsync(user.Stream)) != null)
                {
                    if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    if (message.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase))
                    {
                        await HandlePrivateMessageAsync(user, message);
                    }
                    else
                    {
                        await BroadcastAsync($"[{user.Username}]: {message}", user);
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

                ConsoleLogger.Info($"{user.Username} ({endpoint}) has disconnected");
                await BroadcastAsync($"[SERVER] {user.Username} has left the room!");
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

        private async Task HandlePrivateMessageAsync(User sender, string input)
        {
            var parts = input["/pm ".Length..].Trim().Split(' ', 2);
            if (parts.Length < 2)
            {
                await sender.Writer.WriteLineAsync("[SERVER] Sai cú pháp! Dùng: /pm Tên Tin nhắn");
                return;
            }

            // KHÔNG CHO GUEST GỬI PRIVATE
            if (sender.UserId == 0)
            {
                await sender.Writer.WriteLineAsync("[SERVER] Guest không được gửi tin nhắn riêng!");
                return;
            }


            string targetName = parts[0];
            string msg = parts[1];

            if (_clients.TryGetValue(targetName, out User? target))
            {
                string privateMsg = $"[PRIVATE từ {sender.Username}]: {msg}";
                await target.Writer.WriteLineAsync(privateMsg);
                await sender.Writer.WriteLineAsync($"[Gửi riêng → {targetName}]: {msg}");
                ConsoleLogger.Private($"{sender.Username} → {targetName}: {msg}");
                await _db.SaveMessageAsync(sender.UserId, target.UserId, msg);

            }
            else
            {
                await sender.Writer.WriteLineAsync($"[SERVER] Không tìm thấy người dùng: {targetName}");
            }
        }

        // Đổi Broadcast thành async để không block thread
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
                    // Client đã ngắt, sẽ được dọn ở finally
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
                sb.AppendLine($" - {u.Username} ({u.Client.Client.RemoteEndPoint})");
            }
            sb.Append("============================");
            await BroadcastAsync(sb.ToString());
        }
    }
}
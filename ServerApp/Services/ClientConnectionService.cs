// ServerApp/Services/ClientConnectionService.cs
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using ServerApp.Models;
using ServerApp.Utilities;

namespace ServerApp.Services
{
    public class ClientConnectionService
    {
        private readonly ConcurrentDictionary<string, User> _clients = new();

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

                // Yêu cầu nhập username
                await user.Writer.WriteLineAsync("Please enter your username:");
                await user.Writer.FlushAsync();

                // Đọc username
                string? username = await NetworkHelper.SafeReadLineAsync(user.Stream);

                if (string.IsNullOrWhiteSpace(username))
                    username = "Guest";
                else
                    username = username.Trim();

                // Xử lý trùng tên
                // Vòng lặp xử lý trùng tên - cho người dùng chọn
                while (true)
                {
                    // Kiểm tra tên đã tồn tại chưa
                    if (!_clients.ContainsKey(username))
                    {
                        // Tên hợp lệ, thoát vòng lặp
                        user.Username = username;
                        _clients.TryAdd(username, user);
                        break;
                    }

                    // Tên đã tồn tại → đề xuất tên mới
                    string suggestedName = GenerateSuggestedName(username);

                    await user.Writer.WriteLineAsync($"""
                        [SERVER] The username "{username}" is already taken!
                        Suggested: {suggestedName}

                        Choose an option:
                        1. Enter a new username
                        2. Use suggested username ({suggestedName})
                        Type 1 or 2:
                        """);
                    await user.Writer.FlushAsync();

                    string? choice = await NetworkHelper.SafeReadLineAsync(user.Stream);
                    if (choice == "2")
                    {
                        username = suggestedName;
                        user.Username = username;
                        _clients.TryAdd(username, user);
                        await user.Writer.WriteLineAsync($"[SERVER] Your username is now: {username}");
                        break;
                    }
                    else if (choice == "1" || string.IsNullOrEmpty(choice))
                    {
                        await user.Writer.WriteLineAsync("Enter a new username:");
                        await user.Writer.FlushAsync();

                        string? newName = await NetworkHelper.SafeReadLineAsync(user.Stream);
                        if (string.IsNullOrWhiteSpace(newName))
                        {
                            await user.Writer.WriteLineAsync("[SERVER] Username cannot be empty! Try again.");
                            await user.Writer.FlushAsync();
                            continue;
                        }
                        username = newName.Trim();
                    }
                    else
                    {
                        await user.Writer.WriteLineAsync("[SERVER] Invalid option! Please type 1 or 2.");
                        await user.Writer.FlushAsync();
                    }
                }

                    // 2. Thông báo join
                    ConsoleLogger.Join($"{user.Username} ({endpoint}) has joined");
                await BroadcastAsync($"[SERVER] {user.Username} has joined the room! (Online: {_clients.Count})");
                await BroadcastUserListAsync();

                await user.Writer.WriteLineAsync("===LOGIN_SUCCESS==="); // Dấu hiệu cho client biết đã login OK
                await user.Writer.FlushAsync();

                // 3. Gửi hướng dẫn
                await user.Writer.WriteLineAsync(
                    $"Welcome {user.Username}! Type /pm <username> <message> for private chat, or 'exit' to leave.");
                await user.Writer.FlushAsync();

                // 4. Vòng lặp nhận tin nhắn
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
                // 5. Dọn dẹp
                _clients.TryRemove(user.Username, out _);

                ConsoleLogger.Info($"{user.Username} ({endpoint}) has disconected");
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

            string targetName = parts[0];
            string msg = parts[1];

            if (_clients.TryGetValue(targetName, out User? target))
            {
                string privateMsg = $"[PRIVATE từ {sender.Username}]: {msg}";
                await target.Writer.WriteLineAsync(privateMsg);
                await sender.Writer.WriteLineAsync($"[Gửi riêng → {targetName}]: {msg}");
                ConsoleLogger.Private($"{sender.Username} → {targetName}: {msg}");
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
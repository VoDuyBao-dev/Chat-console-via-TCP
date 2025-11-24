using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerApp.Models;

namespace ServerApp
{
    public class Server
    {
        private TcpListener _listener;
        private readonly List<User> _clients = new();
        private readonly object _lock = new();
        private bool _running = false;
        public int Port { get; }

        public Server(int port = 5000) => Port = port;

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            _running = true;

            PrintLocalIPs();
            Console.WriteLine($"[SERVER] Đang chạy trên port {Port} - Chờ kết nối...");

            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Thread t = new(() => HandleClient(client))
                    {
                        IsBackground = true
                    };
                    t.Start();
                }
                catch (Exception ex) when (!_running)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi Accept: {ex.Message}");
                }
            }
        }

        private void HandleClient(TcpClient tcpClient)
        {
            var user = new User(tcpClient);
            string? clientEndPoint = tcpClient.Client.RemoteEndPoint?.ToString();

            try
            {
                // lấy username(dùng cách low-level) 
                string? username = SafeReadLine(user.Stream);
                user.Username = string.IsNullOrWhiteSpace(username) ? "Guest" : username.Trim();

                lock (_lock) _clients.Add(user);
                Console.WriteLine($"[JOIN] {user.Username} ({clientEndPoint})");

                Broadcast($"[SERVER] {user.Username} đã vào phòng! (Online: {_clients.Count})");
                BroadcastUserList();

                user.Writer.WriteLine($"Chào {user.Username}! Gõ exit để thoát.");

                // nhận tin nhắn
                string? message;
                while ((message = SafeReadLine(user.Stream)) != null)
                {
                    if (message.Equals("exit", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (string.IsNullOrWhiteSpace(message))
                        continue;

                    string fullMsg = $"[{user.Username}]: {message}";
                    Console.WriteLine(fullMsg);
                    Broadcast(fullMsg, user);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {clientEndPoint}: {ex.Message}");
            }
            finally
            {
                lock (_lock) _clients.Remove(user);

                Console.WriteLine($"[LEAVE] {user.Username} ({clientEndPoint}) - Còn lại: {_clients.Count}");
                Broadcast($"[SERVER] {user.Username} đã thoát!");
                BroadcastUserList();

                tcpClient.Close();
            }
        }

        // hàm đọc dòng để đảm bảo an toàn
        private string? SafeReadLine(NetworkStream stream)
        {
            if (!stream.CanRead) return null;

            var sb = new StringBuilder();
            var buffer = new byte[1];
            var newlineCount = 0;

            while (stream.Read(buffer, 0, 1) > 0)
            {
                char c = (char)buffer[0];

                if (c == '\n')
                {
                    newlineCount++;
                    if (newlineCount == 1) break; // \n kết thúc
                }
                else if (c != '\r')
                {
                    sb.Append(c);
                    newlineCount = 0;
                }

                // Bảo vệ chống DoS (tin nhắn quá dài)
                if (sb.Length > 10_000)
                {
                    Console.WriteLine("Cảnh báo: Tin nhắn quá dài, bị cắt!");
                    break;
                }
            }

            return sb.Length == 0 ? null : sb.ToString();
        }

        private void Broadcast(string message, User? exclude = null)
        {
            lock (_lock)
            {
                foreach (var client in _clients.ToList())
                {
                    if (client == exclude) continue;
                    try
                    {
                        client.Writer.WriteLine(message);
                    }
                    catch
                    {
                        // Client đã mất kết nối → sẽ được dọn ở finally
                    }
                }
            }
        }

        private void BroadcastUserList()
        {
            lock (_lock)
            {
                var list = new StringBuilder("========== Online ==========\n");
                foreach (var u in _clients)
                    list.AppendLine($" - {u.Username} ({u.Client.Client.RemoteEndPoint})");
                list.Append("============================");
                Broadcast(list.ToString());
            }
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
            Broadcast("[SERVER] Server đang tắt...");
            lock (_lock)
            {
                foreach (var c in _clients) c.Client.Close();
                _clients.Clear();
            }
        }

        private void PrintLocalIPs()
        {
            Console.WriteLine("\nIP để kết nối từ máy khác trong LAN:");

            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    Console.WriteLine($" → {ip}:{Port}");

            Console.WriteLine();
        }



    }
}

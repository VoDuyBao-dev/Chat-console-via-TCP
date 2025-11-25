using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerApp.Models;
using ServerApp.Services;
using Common;


namespace ServerApp
{
    public class Server
    {
        private TcpListener _listener;
        private readonly List<User> _clients = new();
        private readonly object _lock = new();
        private bool _running = false;
        public int Port { get; }
        private readonly DatabaseService _db;


        // public Server(int port = 5000) => Port = port;

        public Server(int port = 5000)
        {
            Port = port;
            // Kết nối đến database 
            string conn = ConfigService.GetConnectionString();
            _db = new DatabaseService(conn);
        }



        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            _running = true;

            PrintLocalIPs();
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine($"[SERVER] Đang chạy trên port {Port} - Chờ kết nối...");

            while (_running)
            {
                try
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    // hiển thị kết nối thành công 
                    string? remote = client.Client.RemoteEndPoint?.ToString();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[CONNECT] Client đã kết nối: {remote}");
                    Console.ResetColor();

                    Thread t = new(() => HandleClient(client))
                    {
                        IsBackground = true
                    };
                    t.Start();
                }
                catch (Exception ex) when (!_running)
                {
                    Console.WriteLine(ex.Message);
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
                // user.Writer.WriteLine("WELCOME|Hãy REGISTER hoặc LOGIN");
                // user.Writer.WriteLine("FORMAT: REGISTER|username|password");
                // user.Writer.WriteLine("FORMAT: LOGIN|username|password");
                lock (_lock) _clients.Add(user);

                while (true)
                {
                    string? raw = SafeReadLine(user.Stream);
                    if (raw == null) return;

                    var parts = Protocol.Decode(raw);
                    string cmd = parts[0].ToUpper();

                    switch (cmd)
                    {
                        case "REGISTER":
                            HandleRegister(user, parts);
                            break;

                        case "LOGIN":
                            if (HandleLogin(user, parts))
                                goto LoggedIn;
                            break;

                        default:
                            user.Writer.WriteLine("ERROR|Hãy LOGIN hoặc REGISTER.");
                            break;
                    }
                }

            LoggedIn:
                user.Writer.WriteLine(" Gõ exit để thoát.");

                string? message;
                while ((message = SafeReadLine(user.Stream)) != null)
                {
                    if (message == "exit") break;
                    if (string.IsNullOrWhiteSpace(message)) continue;

                    Broadcast($"[{user.Username}]: {message}", user);

                    int? senderId = _db.GetUserId(user.Username);
                    _db.SaveMessage(senderId, null, message, 1);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {clientEndPoint}: {ex.Message}");
            }
            finally
            {
                if (user.Username != null)
                {
                    _db.SetOffline(user.Username);
                }
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




        private void HandleRegister(User user, string[] parts)
        {
            if (parts.Length < 3)
            {
                user.Writer.WriteLine($"{Protocol.REGISTER_FAIL}|Invalid Format");
                return;
            }

            string username = parts[1];
            string password = parts[2];

            if (_db.IsUsernameTaken(username))
            {
                user.Writer.WriteLine($"{Protocol.REGISTER_FAIL}|Username Exists");
                return;
            }

            bool ok = _db.CreateUser(username, Utils.HashPassword(password), username);

            if (ok)
                user.Writer.WriteLine($"{Protocol.REGISTER_OK}");
            else
                user.Writer.WriteLine($"{Protocol.REGISTER_FAIL}|DB_ERROR");
        }

        private bool HandleLogin(User user, string[] parts)
        {
            if (parts.Length < 3)
            {
                user.Writer.WriteLine($"{Protocol.LOGIN_FAIL}|Invalid Format");
                return false;
            }

            string username = parts[1];
            string password = parts[2];

            if (!_db.ValidateUser(username, password))
            {
                user.Writer.WriteLine($"{Protocol.LOGIN_FAIL}|Sai tài khoản hoặc mật khẩu");
                return false;
            }

            user.Username = username;
            _db.SetOnline(username);

            lock (_lock) _clients.Add(user);

            // Gửi đúng chuẩn giao thức
            user.Writer.WriteLine($"{Protocol.LOGIN_OK}|{username}");

            // Broadcast($"[SERVER] {username} đã vào phòng!");
            // BroadcastUserList();
            return true;
        }




    }
}

using Common;
using System;
using System.Threading.Tasks;
using ClientApp.Services;
using ClientApp.Utilities;

namespace ClientApp
{
    public class Client
    {
        private readonly UdpDiscoveryClient _discovery = new();
        private readonly TcpChatClient _chat = new();

        public async Task RunAsync()
        {
            Console.Title = "Chat LAN Client - Nhóm 11";
            ConsoleLogger.Info("=== CHÀO MỪNG ĐẾN VỚI CHAT LAN ===");

            // Bước 1: Tìm server
            var servers = await _discovery.DiscoverServersAsync(TimeSpan.FromSeconds(8));

            string serverIp;
            int serverPort;

            if (servers.Count == 0)
            {
                ConsoleLogger.Info("Không tìm thấy server tự động. Nhập thủ công:");
                Console.Write("IP Server: ");
                serverIp = Console.ReadLine()!.Trim();
                Console.Write("Port (mặc định 5000): ");
                var portInput = Console.ReadLine();
                serverPort = string.IsNullOrEmpty(portInput) ? 5000 : int.Parse(portInput);
            }
            else
            {
                ConsoleLogger.Info($"\nTìm thấy {servers.Count} server:");
                for (int i = 0; i < servers.Count; i++)
                    Console.WriteLine($"  [{i + 1}] {servers[i]}");

                Console.Write("\nChọn server (nhập số): ");
                if (!int.TryParse(Console.ReadLine(), out int choice) || choice < 1 || choice > servers.Count)
                    choice = 1;

                var selected = servers[choice - 1];
                serverIp = selected.Ip;
                serverPort = selected.Port;
                ConsoleLogger.Success($"Đã chọn: {selected.Name}");
            }

            // Bước 2: Kết nối TCP
            await _chat.ConnectAsync(serverIp, serverPort);

            var session = new UserSession();

            // Bắt đầu nhận tin nhắn từ server (duy nhất 1 lần)
            _chat.StartReceiving(message =>
            {
                if (message.Contains("===LOGIN_SUCCESS==="))
                {
                    ConsoleLogger.Success("Đăng nhập thành công! Bạn đã vào phòng chat.\n");
                    session.Login("unknown"); // tên thật in trong message khác, tạm vậy
                    // return;
                }

                if (message == "[DISCONNECTED]")
                {
                    ConsoleLogger.Error("Mất kết nối với server.");
                    session.IsRunning = false;
                    return;
                }

                if (message.StartsWith("[SERVER]"))
                    ConsoleLogger.Info(message);
                else if (message.StartsWith("[PRIVATE", StringComparison.OrdinalIgnoreCase))
                    ConsoleLogger.Private(message);
                else
                    ConsoleLogger.Receive(message);
            });

            // ===== VÒNG LẶP MENU Ở CLIENT =====
            while (!session.IsLoggedIn && session.IsRunning)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("""
                ===============================
                [1] Đăng ký tài khoản
                [2] Đăng nhập
                [0] Vào với tư cách Guest
                ===============================
                """);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Chọn: ");
                var opt = Console.ReadLine();

                switch (opt)
                {
                    case "1":
                        await HandleRegisterAsync();
                        ConsoleLogger.Info("Đăng ký xong. Bạn có thể chọn [2] để đăng nhập.");
                        break;

                    case "2":
                        await HandleLoginAsync();
                        ConsoleLogger.Info("Đang chờ server xác nhận đăng nhập...");
                        // chờ callback StartReceiving set IsLoggedIn = true
                        await Task.Delay(500);
                        break;

                    case "0":
                        await _chat.SendMessageAsync("GUEST");
                        ConsoleLogger.Info("Đang vào phòng chat với tư cách Guest...");
                        await Task.Delay(500);
                        break;

                    default:
                        ConsoleLogger.Error("Lựa chọn không hợp lệ. Vui lòng chọn 0,1,2.");
                        break;
                }
            }

            if (!session.IsRunning)
            {
                ConsoleLogger.Error("Kết nối đã bị đóng. Thoát client.");
                return;
            }

            // ===== VÒNG LẶP CHAT CHÍNH =====
            ConsoleLogger.Info("Gõ tin nhắn, dùng /pm <tên> <nội dung>, hoặc 'exit' để thoát.\n");
            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null) continue;

                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    await _chat.SendMessageAsync("exit");
                    break;
                }

                await _chat.SendMessageAsync(input);
            }

            _chat.Disconnect();
        }

        // ============ VALIDATION HỖ TRỢ ===============
        private bool IsInvalid(string? input) => string.IsNullOrWhiteSpace(input);

        private bool ContainsIllegalChars(string input)
        {
            char[] illegal = { '|', '/', '\\', ':', ';', '\'', '"', '<', '>', '{', '}', '[', ']' };
            return input.Any(c => illegal.Contains(c));
        }

        // ============ REGISTER ===============
        private async Task HandleRegisterAsync()
        {
            string u, p, d;

            // Username
            while (true)
            {
                Console.Write("Username: ");
                u = Console.ReadLine()!.Trim();

                if (IsInvalid(u))
                {
                    ConsoleLogger.Error("Username không được bỏ trống.");
                    continue;
                }
                if (u.Length < 3)
                {
                    ConsoleLogger.Error("Username phải có ít nhất 3 ký tự.");
                    continue;
                }
                if (ContainsIllegalChars(u))
                {
                    ConsoleLogger.Error("Username chứa ký tự không hợp lệ.");
                    continue;
                }
                break;
            }

            // Password
            while (true)
            {
                Console.Write("Password: ");
                p = Console.ReadLine()!;

                if (IsInvalid(p))
                {
                    ConsoleLogger.Error("Password không được bỏ trống.");
                    continue;
                }
                if (p.Length < 6)
                {
                    ConsoleLogger.Error("Password phải có ít nhất 6 ký tự.");
                    continue;
                }
                break;
            }

            // Display name
            while (true)
            {
                Console.Write("Tên hiển thị: ");
                d = Console.ReadLine()!.Trim();

                if (IsInvalid(d))
                {
                    ConsoleLogger.Error("Tên hiển thị không được bỏ trống.");
                    continue;
                }
                if (d.Length < 2)
                {
                    ConsoleLogger.Error("Tên hiển thị phải dài hơn 1 ký tự.");
                    continue;
                }
                if (ContainsIllegalChars(d))
                {
                    ConsoleLogger.Error("Tên hiển thị chứa ký tự không hợp lệ.");
                    continue;
                }
                break;
            }

            string passHash = Utils.PasswordHasher.SHA256Hash(p); // dùng Utils.HashPassword trong Common
            await _chat.SendMessageAsync($"REGISTER|{u}|{passHash}|{d}");
        }

        // ============ LOGIN ===============
        private async Task HandleLoginAsync()
        {
            string u, p;

            while (true)
            {
                Console.Write("Username: ");
                u = Console.ReadLine()!.Trim();

                if (IsInvalid(u))
                {
                    ConsoleLogger.Error("Username không được bỏ trống.");
                    continue;
                }
                if (ContainsIllegalChars(u))
                {
                    ConsoleLogger.Error("Username chứa ký tự không hợp lệ.");
                    continue;
                }
                break;
            }

            while (true)
            {
                Console.Write("Password: ");
                p = Console.ReadLine()!;

                if (IsInvalid(p))
                {
                    ConsoleLogger.Error("Password không được bỏ trống.");
                    continue;
                }
                break;
            }

            string passHash = Utils.PasswordHasher.SHA256Hash(p);
            await _chat.SendMessageAsync($"LOGIN|{u}|{passHash}");
        }
    }
}

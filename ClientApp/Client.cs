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

            await _chat.ConnectAsync(serverIp, serverPort);

            var session = new UserSession();
            var register = new RegisterService(_chat);
            var login = new LoginService(_chat);

            _chat.StartReceiving(message =>
            {
                if (message.Contains("===LOGIN_SUCCESS==="))
                {
                    ConsoleLogger.Success("Đăng nhập thành công! Bạn đã vào phòng chat.\n");
                    session.Login("unknown");
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

            // MENU
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
                        await register.HandleRegisterAsync();
                        // ConsoleLogger.Info("Đăng ký xong. Bạn có thể chọn [2] để đăng nhập.");
                        break;

                    case "2":
                        await login.HandleLoginAsync();
                        ConsoleLogger.Info("Đang chờ server xác nhận đăng nhập...");
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

            // CHAT LOOP 
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
    }
}

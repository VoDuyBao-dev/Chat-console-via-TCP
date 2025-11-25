using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
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
            // Bắt đầu nhận tin nhắn - CHỈ GỌI 1 LẦN DUY NHẤT!
            _chat.StartReceiving(message =>
            {
                // Tất cả tin nhắn từ server đều đi qua đây → bạn làm gì cũng được!
                if (message.StartsWith("[PRIVATE", StringComparison.OrdinalIgnoreCase))
                    ConsoleLogger.Private(message);
                else if (message.Contains("===LOGIN_SUCCESS===")) // server gửi cái này là login OK
                    ConsoleLogger.Success("Đăng nhập thành công!\n");
                else if (message.Contains("has joined") || message.Contains("Welcome") || message.Contains("Your username is now"))
                    ConsoleLogger.Success(message);
                else if (message.StartsWith("[SERVER]"))
                    ConsoleLogger.Info(message);
                else
                    ConsoleLogger.Receive(message);
            });

            // Đăng nhập: server hỏi gì thì in, người dùng nhập gì thì gửi
            ConsoleLogger.Info("Đang chờ server yêu cầu tên...");
            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null) continue;

                await _chat.SendMessageAsync(input.Trim());

                // Dừng vòng lặp khi thấy dấu hiệu login thành công
                // (Bạn có thể để server gửi một dòng đặc biệt như ===LOGIN_SUCCESS===)
                // Hoặc kiểm tra tin nhắn gần nhất có chứa từ khóa join/welcome
                // Cách đơn giản: đợi 1 lúc rồi break (vì tin nhắn welcome đã được in bởi callback)
                await Task.Delay(800); // đợi server phản hồi
                ConsoleLogger.Success("Đăng nhập thành công! Bắt đầu chat...\n");
                break;
            }

            // Chat chính
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
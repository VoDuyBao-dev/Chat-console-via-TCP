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

                string serverIp;
                int serverPort;

                // ... (Phần tìm server không thay đổi) ...
                var servers = await _discovery.DiscoverServersAsync(TimeSpan.FromSeconds(4));

                if (servers.Count == 0)
                {
                    ConsoleLogger.Info("No server found. Enter manually:");
                    Console.Write("IP Server: ");
                    serverIp = Console.ReadLine()!.Trim();
                    Console.Write("Port (default 5000): ");
                    var portInput = Console.ReadLine();
                    serverPort = string.IsNullOrEmpty(portInput) ? 5000 : int.Parse(portInput);
                }
                else
                {
                    ConsoleLogger.Info($"\nFound {servers.Count} server(s):");
                    for (int i = 0; i < servers.Count; i++)
                        Console.WriteLine($"  [{i + 1}] {servers[i]}");

                    int choice = 0;
                    while (true)
                    {
                        Console.Write("\nChoose server (index): ");
                        var input = Console.ReadLine();

                        if (!int.TryParse(input, out choice) ||
                            choice < 1 || choice > servers.Count)
                        {
                            ConsoleLogger.Error("Invalid selection! Please enter a valid number from the list");
                            continue;
                        }
                        break;
                    }

                    var selected = servers[choice - 1];
                    serverIp = selected.Ip;
                    serverPort = selected.Port;
                    ConsoleLogger.Success($"Selected: {selected.Name}");
                }

                // Kết nối tới server
                try
                {
                    await _chat.ConnectAsync(serverIp, serverPort);
                }
                catch
                {
                    ConsoleLogger.Error("Không thể kết nối đến server!");
                    Console.WriteLine(); // <<< THÊM: Tạo dòng trống trước menu Reconnect
                    
                    bool ok = await ReconnectLoopAsync();
                    if (!ok) return;

                    // cập nhật lại server info sau khi reconnect
                    var newServers = await _discovery.DiscoverServersAsync(TimeSpan.FromSeconds(2));
                    serverIp = newServers[0].Ip;
                    serverPort = newServers[0].Port;
                }

                var session = new UserSession();
                var register = new RegisterService(_chat);
                var login = new LoginService(_chat);

                _chat.StartReceiving(async message =>
                {
                    if (message.Contains("LOGIN_SUCCESS"))
                    {
                        ConsoleLogger.Success("Login OK! Welcome to the chat room.\n");
                        session.Login("unknown");
                        return;
                    }

                    if (message.Contains("REGISTER_SUCCESS"))
                    {
                        ConsoleLogger.Success("Registration successful! You are now logged into the chat room.\n");
                        session.Login("unknown");
                        return;
                    }

                if (message == "[DISCONNECTED]")
                    {
                        ConsoleLogger.Error("Mất kết nối đến server!");
                        Console.WriteLine(); 
                        session.ResetAuthWait();

                        bool ok = await ReconnectLoopAsync();
                        if (!ok)
                        {
                            session.IsRunning = false;
                            return;
                        }

                        // Sau khi reconnect thành công, reset session để menu login hiển thị
                        session.IsWaitingAuth = false;
                        session.IsRunning = true;
                        ConsoleLogger.Info("Bạn có thể đăng nhập lại!");
                        return;
                    }

                    // Nếu CHƯA đăng nhập mà nhận [SERVER] ... thì coi đó là kết quả AUTH (sai username/password,...)
                    if (!session.IsLoggedIn && message.StartsWith("[SERVER]"))
                    {
                        ConsoleLogger.Info(message);
                        session.ResetAuthWait();
                        return;
                    }

                    if (message.StartsWith("[SERVER]"))
                        ConsoleLogger.Info(message);
                    else if (message.StartsWith("[PRIVATE", StringComparison.OrdinalIgnoreCase))
                        ConsoleLogger.Private(message);
                    else
                        ConsoleLogger.Receive(message);
                });

                // MENU đăng nhập / đăng ký
                while (!session.IsLoggedIn && session.IsRunning)
                {
                    if (session.IsWaitingAuth)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("""
                        ===============================
                        [1] Register
                        [2] Login
                        ===============================
                        """);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Select: ");
                    var opt = Console.ReadLine();

                    switch (opt)
                    {
                        case "1":
                            await register.HandleRegisterAsync(maskPassword: true);
                            session.IsWaitingAuth = true;
                            break;

                        case "2":
                            await login.HandleLoginAsync(maskPassword: true);
                            session.IsWaitingAuth = true;
                            break;

                        default:
                            ConsoleLogger.Error("Invalid option.");
                            Console.WriteLine(); // <<< THÊM: Dòng trống sau lỗi menu Auth
                            break;
                    }
                }

                if (!session.IsRunning)
                {
                    ConsoleLogger.Error("Kết nối đã bị đóng. Thoát client.");
                    return;
                }

                // CHAT LOOP (Không thay đổi)
                while (true)
                {
                    string? input = Console.ReadLine();
                    // ... (Các lệnh chat) ...
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    input = input.Trim();

                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                    {
                        await _chat.SendMessageAsync("EXIT");
                        break;
                    }

                    if (input.Equals("users", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("/users", StringComparison.OrdinalIgnoreCase))
                    {
                        await _chat.SendMessageAsync("USERS");
                        continue;
                    }

                    if (input.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                        input.Equals("/help", StringComparison.OrdinalIgnoreCase))
                    {
                        await _chat.SendMessageAsync("HELP");
                        continue;
                    }

                    if (input.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase) ||
                        input.StartsWith("pm ", StringComparison.OrdinalIgnoreCase))
                    {
                        string content = input.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase) ? input[4..].Trim() : input[3..].Trim();
                        var parts = content.Split(' ', 2);
                        if (parts.Length != 2)
                        {
                            ConsoleLogger.Error("Usage: /pm <DisplayName> <message>");
                            continue;
                        }
                        await _chat.SendMessageAsync($"PM|{parts[0]}|{parts[1]}");
                        continue;
                    }

                    if (input.StartsWith("msg ", StringComparison.OrdinalIgnoreCase) ||
                        input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
                    {
                        string content = input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase) ? input[5..].Trim() : input[4..].Trim();
                        if (string.IsNullOrWhiteSpace(content))
                        {
                            ConsoleLogger.Error("Usage: msg <message>");
                            continue;
                        }
                        await _chat.SendMessageAsync($"MSG|{content}");
                        continue;
                    }

                    ConsoleLogger.Error($"Unknown command: {input}. Type /help for commands.");
                }

                _chat.Disconnect();
            }

    private async Task<bool> ReconnectLoopAsync()
{
    while (true)
    {
        Console.WriteLine("\n=======================");
        Console.WriteLine("1. Reconnect (đợi 10s)");
        Console.WriteLine("2. Exit");
        Console.WriteLine("=======================");
        Console.Write("Chọn: ");

        var opt = (Console.ReadLine() ?? "").Trim();

        if (string.IsNullOrEmpty(opt))
            continue; // nhấn Enter sẽ hiển thị lại menu

        if (opt == "2")
        {
            ConsoleLogger.Info("Thoát chương trình...");
            return false;
        }
        else if (opt == "1")
        {
            ConsoleLogger.Info("Đang tìm server trong 10 giây...");
            
            DateTime endTime = DateTime.Now.AddSeconds(10);
            while (DateTime.Now < endTime)
            {
                try
                {
                    var servers = await _discovery.DiscoverServersAsync(TimeSpan.FromMilliseconds(500));
                    if (servers.Count > 0)
                    {
                        var sv = servers[0];
                        ConsoleLogger.Success($"✓ Tìm thấy server: {sv.Name}");
                        await _chat.ConnectAsync(sv.Ip, sv.Port);
                        ConsoleLogger.Success("✓ Reconnect thành công!\n");
                        return true; // reconnect thành công
                    }
                }
                catch { }

                await Task.Delay(500);
            }

            ConsoleLogger.Error("✗ Không tìm thấy server sau 10 giây!\n");
            // menu sẽ hiển thị lại vì while(true)
        }
        else
        {
            ConsoleLogger.Error("❌ Lựa chọn không hợp lệ! Vui lòng chọn 1 hoặc 2.\n");
        }
    }
    }
}
}
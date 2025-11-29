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

        private string _serverIp = "";
        private int _serverPort = 0;

        private bool _needsReconnect = false;
        private bool _isTyping = false;
        private bool _isLoggedIn = false;
        private bool _isWaitingAuth = false;
        private string? _username = null;

        public async Task RunAsync()
        {
            Console.Title = "Chat LAN Client - Nhóm 11";
            ConsoleLogger.Info("=== Welcome to the LAN Chat System===");

            var register = new RegisterService(_chat);
            var login = new LoginService(_chat);

            while (true)
            {
                // Reset flags khi bắt đầu vòng mới
                _needsReconnect = false;
                _isLoggedIn = false;
                _isWaitingAuth = false;

                // ==== TÌM SERVER ====
                var servers = await _discovery.DiscoverServersAsync(TimeSpan.FromSeconds(4));
                if (servers.Count == 0)
                {
                    ConsoleLogger.Info("No server found. Enter manually:");
                    Console.Write("IP Server: ");
                    _serverIp = Console.ReadLine()!.Trim();
                    Console.Write("Port (default 5000): ");
                    var portInput = Console.ReadLine();
                    _serverPort = string.IsNullOrEmpty(portInput) ? 5000 : int.Parse(portInput);
                }
                else
                {
                    ConsoleLogger.Info($"\nFound {servers.Count} server(s):");
                    for (int i = 0; i < servers.Count; i++)
                        Console.WriteLine($"  [{i + 1}] {servers[i]}");

                    int choice;
                    while (true)
                    {
                        Console.Write("\nChoose server (index): ");
                        if (int.TryParse(Console.ReadLine(), out choice) &&
                            choice >= 1 && choice <= servers.Count)
                            break;
                        ConsoleLogger.Error("Invalid selection!");
                    }

                    var selected = servers[choice - 1];
                    _serverIp = selected.Ip;
                    _serverPort = selected.Port;
                    ConsoleLogger.Success($"Selected: {selected.Name}");
                }

                // ==== KẾT NỐI ====
                try
                {
                    _chat.Disconnect(); // Dừng thread cũ trước
                    await Task.Delay(500); // Đợi cleanup
                    await _chat.ConnectAsync(_serverIp, _serverPort);
                }
                catch
                {
                    ConsoleLogger.Error("Unable to connect to the server!");
                    if (!await ReconnectLoopAsync()) return;
                    continue; // Bắt đầu lại từ đầu vòng while
                }

                // ==== NHẬN TIN ====
               _chat.StartReceiving(async message =>
{
    if (_isTyping) return;

    // ---- LOGIN SUCCESS ----
    if (message.StartsWith("LOGIN_SUCCESS"))
    {
        ConsoleLogger.Success("Login successful!\n");
        _isLoggedIn = true;
        _isWaitingAuth = false;
        return;
    }

    // ---- LOGIN FAIL ----
    if (message.StartsWith("LOGIN_FAIL"))
    {
        var reason = message.Contains('|') ? message.Split('|')[1] : "Unknown error";
        ConsoleLogger.Error("Login failed: " + reason);
        _isWaitingAuth = false;
        _isLoggedIn = false;
        return;
    }

    // ---- REGISTER SUCCESS ----
    if (message.StartsWith("REGISTER_SUCCESS"))
    {
        ConsoleLogger.Success("Register successful!\n");
        _isLoggedIn = true;
        _isWaitingAuth = false;
        return;
    }

    // ---- REGISTER FAIL ----
    if (message.StartsWith("REGISTER_FAIL"))
    {
        var reason = message.Contains('|') ? message.Split('|')[1] : "Unknown error";
        ConsoleLogger.Error("Register failed: " + reason);
        _isWaitingAuth = false;
        _isLoggedIn = false;
        return;
    }

    // ---- DISCONNECTED ----
    if (message == "[DISCONNECTED]")
    {
        ConsoleLogger.Error("Lost connection to the server!\n");
        _needsReconnect = true;
        return;
    }

    // ---- OTHER MESSAGES ----
// ---- USERS LIST ----
    if (message.StartsWith("USERS|"))
    {
        var list = message.Substring("USERS|".Length).Split(',');
        ConsoleLogger.Info("Online users:");
        foreach (var user in list)
            Console.WriteLine(" - " + user);
        return;
    }

    // ---- HELP ----
    if (message.StartsWith("HELP|"))
    {
        var helpText = message.Substring("HELP|".Length).Replace("|", "\n");
        ConsoleLogger.Info("Help commands:\n" + helpText);
        return;
    }

    // ---- PUBLIC MESSAGE ----
    if (message.StartsWith("MSG|") || message.StartsWith("BROADCAST|"))
    {
        var content = message.Contains('|') ? message.Split('|', 2)[1] : message;
        ConsoleLogger.Receive(content);
        return;
    }

    // ---- PRIVATE MESSAGE ----
    if (message.StartsWith("PM|"))
    {
        var parts = message.Split('|', 3);
        if (parts.Length >= 3)
        {
            string from = parts[1];
            string msg = parts[2];
            ConsoleLogger.Private($"[PM FROM {from}] {msg}");
        }
        return;
    }

    // ---- SERVER MESSAGE / DEFAULT ----
    if (message.StartsWith("[SERVER]"))
    {
        ConsoleLogger.Info(message);
        return;
    }

    // In ra message nếu không khớp gì
    ConsoleLogger.Info(message);
});


                // ==== AUTH LOOP ====
                while (!_isLoggedIn && !_needsReconnect)
                {
                    if (_isWaitingAuth)
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
                            await register.HandleRegisterAsync(true);
                            _isWaitingAuth = true;
                            break;
                        case "2":
                            await login.HandleLoginAsync(true);
                            _isWaitingAuth = true;
                            break;
                        default:
                            ConsoleLogger.Error("Invalid option.");
                            break;
                    }
                }

                // Nếu cần reconnect trong auth loop, bỏ qua chat loop
                if (_needsReconnect)
                {
                    if (!await ReconnectLoopAsync()) return;
                    continue; // Quay lại đầu vòng while chính
                }

                // ==== CHAT LOOP ====
                while (_isLoggedIn && _chat.IsConnected && !_needsReconnect)
                {
                    string? input = Console.ReadLine();
                    
                    // Kiểm tra reconnect ngay sau ReadLine
                    if (_needsReconnect) break;
                    if (string.IsNullOrWhiteSpace(input)) continue;
                    input = input.Trim();

                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        await _chat.SendMessageAsync("EXIT");
                        _chat.Disconnect();
                        return; // Thoát app hoàn toàn
                    }
                    if (input.Equals("users", StringComparison.OrdinalIgnoreCase))
                    {
                        await _chat.SendMessageAsync("USERS");
                        continue;
                    }
                    if (input.Equals("help", StringComparison.OrdinalIgnoreCase))
                    {
                        await _chat.SendMessageAsync("HELP");
                        continue;
                    }
                    if (input.StartsWith("/pm "))
                    {
                        var parts = input[4..].Trim().Split(' ', 2);
                        if (parts.Length != 2)
                        {
                            ConsoleLogger.Error("Usage: /pm <name> <msg>");
                            continue;
                        }
                        await _chat.SendMessageAsync($"PM|{parts[0]}|{parts[1]}");
                        continue;
                    }
                    if (input.StartsWith("/msg "))
                    {
                        await _chat.SendMessageAsync($"MSG|{input[5..].Trim()}");
                        continue;
                    }
                    ConsoleLogger.Error("Unknown command.");
                }

                // Disconnect cleanup
                _chat.Disconnect();
                await Task.Delay(500);

                // Xử lý reconnect nếu cần
                if (_needsReconnect)
                {
                    if (!await ReconnectLoopAsync()) return;
                    continue; // Quay lại đầu vòng while chính
                }
                
                // Nếu không reconnect thì thoát app
                break;
            }
        }

        // ===== RECONNECT LOOP =====
        private async Task<bool> ReconnectLoopAsync()
        {
            while (true)
            {
                Console.WriteLine("\n=======================");
                Console.WriteLine("1. Reconnect");
                Console.WriteLine("2. Exit");
                Console.WriteLine("=======================");
                Console.Write("Choose: ");
                var opt = (Console.ReadLine() ?? "").Trim();

                if (opt == "2")
                {
                    ConsoleLogger.Info("Exiting application...");
                    return false;
                }

                if (opt == "1")
                {
                    ConsoleLogger.Info("Reconnecting...");
                    
                    int retries = 0;
                    const int maxRetries = 3;
                    
                    while (retries < maxRetries)
                    {
                        try
                        {
                            _chat.Disconnect();
                            await Task.Delay(1000);
                            
                            await _chat.ConnectAsync(_serverIp, _serverPort, showLog: false);
                            ConsoleLogger.Success("Reconnected successfully!");
                            
                            // Reset các flag trạng thái
                            _isLoggedIn = false;
                            _isWaitingAuth = false;
                            _needsReconnect = false;
                            
                            return true;
                        }
                        catch
                        {
                            retries++;
                            if (retries < maxRetries)
                            {
                                ConsoleLogger.Error($"Reconnect failed. Retry {retries}/{maxRetries} in 5 seconds...");
                                await Task.Delay(5000);
                            }
                            else
                            {
                                ConsoleLogger.Error("Max retries reached. Please try again.");
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ConsoleLogger.Error("Invalid option. Please choose 1 or 2.");
                }
            }
        }
    }
}
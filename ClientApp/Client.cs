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

            var servers = await _discovery.DiscoverServersAsync(TimeSpan.FromSeconds(4));

            string serverIp;
            int serverPort;

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
                    Console.WriteLine($"  [{i + 1}] {servers[i]}");

                int choice = 0;
                while (true)
                {
                    Console.Write("\nChoose server (index): ");
                    var input = Console.ReadLine();

                    if (!int.TryParse(input, out choice) ||
                        choice < 1 ||
                        choice > servers.Count)
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

            await _chat.ConnectAsync(serverIp, serverPort);

            var session = new UserSession();
            var register = new RegisterService(_chat);
            var login = new LoginService(_chat);

            _chat.StartReceiving(message =>
            {
                //Thông báo từ server 

                if (message == "[DISCONNECTED]")
                {
                    ConsoleLogger.Error("Disconnected from server.");
                    session.IsRunning = false;
                    return;
                }

                if (message.Contains("LOGIN_SUCCESS"))
                {
                    var parts = message.Split('|');
                    if (parts.Length >= 3)
                    {
                        string username = parts[1];
                        string display = parts[2];

                        session.Login(username, display);
                        ConsoleLogger.Success($"Logged in as {display}");
                    }
                    else
                    {
                        session.Login("unknown", "unknown");
                    }
                    ConsoleLogger.Info(message);
                    session.IsWaitingAuth = false;
                    session.IsLoggedIn = true;

                    return;
                }

                if (message.Contains("REGISTER_SUCCESS"))
                {
                    var parts = message.Split('|');
                    if (parts.Length >= 3)
                    {
                        string username = parts[1];
                        string display = parts[2];

                        session.Login(username, display);
                        ConsoleLogger.Success($"Account created. Welcome {display}!");
                    }
                    else
                    {
                        session.Login("unknown", "unknown");
                    }
                    ConsoleLogger.Info(message);
                    session.IsWaitingAuth = false;
                    session.IsLoggedIn = true;
                    return;
                }


                if (!session.IsLoggedIn && message.StartsWith("[SERVER]"))
                {
                    // In thông báo lỗi từ server
                    ConsoleLogger.Info(message);
                    // Cho phép menu hiện lại
                    session.ResetAuthWait();
                    return;
                }

                if (message.StartsWith("[privatechat_ok] enter", StringComparison.OrdinalIgnoreCase))
                {
                    session.InPrivateChat = true;

                    ConsoleLogger.Private("Entered private chat. oke");
                    return;

                }

                if (message.StartsWith("[privatechat_ok]", StringComparison.OrdinalIgnoreCase))
                {
                    session.InPrivateChat = true;

                    ConsoleLogger.Private("Entered private chat.");
                    return;

                }


                // Server báo lỗi PM
                if (message.StartsWith("privatechat_error", StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleLogger.Error(message);
                    session.InPrivateChat = false;
                    session.PrivateChatTarget = null;
                    return;
                }
                if (session.InPrivateChat)
                {
                    if (message.StartsWith("[PM FROM"))
                    {
                        // người kia gửi
                        ConsoleLogger.Private(TextAlign.AlignLeft(message));
                    }
                    else if (message.StartsWith("[PM TO"))
                    {
                        // mình gửi
                        ConsoleLogger.Private(TextAlign.AlignRight(message));
                    }
                    else
                    {
                        // thông báo trong phòng PM
                        ConsoleLogger.Private(message);
                    }
                    return;
                }


                ConsoleLogger.Receive(message);
            });

            // MENU
            while (!session.IsLoggedIn && session.IsRunning)
            {
                // Nếu đang chờ server trả lời, không in menu thêm, chỉ đợi
                if (session.IsWaitingAuth)
                {
                    await Task.Delay(100); // tránh busy-wait
                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("""
                    ===============================
                    [1] Register
                    [2] Login
                    [3] Exit
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
                        ConsoleLogger.Info("Waiting for server response...");
                        break;

                    case "2":
                        await login.HandleLoginAsync(maskPassword: true);
                        session.IsWaitingAuth = true;
                        ConsoleLogger.Info("Waiting for server response...");
                        break;
                    case "3":
                        session.IsRunning = false;
                        await _chat.SendMessageAsync("EXIT");
                        break;

                    default:
                        ConsoleLogger.Error("Invalid option.");
                        break;
                }
            }


            if (!session.IsRunning)
            {
                ConsoleLogger.Error("Kết nối đã bị đóng. Thoát client.");
                return;
            }

            // CHAT LOOP 
            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null) continue;

                input = input.Trim();
                if (input == "") continue;
                if (input.Equals("/exitpm", StringComparison.OrdinalIgnoreCase) || input.Equals("exitpm", StringComparison.OrdinalIgnoreCase))
                {
                    if (!session.InPrivateChat)
                    {
                        ConsoleLogger.Error("You are not in private chat hehe.");
                        continue;
                    }

                    await _chat.SendMessageAsync(MessageBuilder.ExitPrivateRoom());
                    session.InPrivateChat = false;
                    session.PrivateChatTarget = null;

                    ConsoleLogger.Info("Exited private chat.");
                    continue;
                }
                if (session.InPrivateChat &&
                session.PrivateChatTarget != null &&
                session.Username != null &&
                session.DisplayName != null &&
                session.IsLoggedIn)
                {
                    await _chat.SendMessageAsync(MessageBuilder.PrivateMessage(input));
                    continue;
                }


                // EXIT
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    await _chat.SendMessageAsync(MessageBuilder.Exit());
                    break;
                }



                if (input.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase) ||
                   input.StartsWith("pm ", StringComparison.OrdinalIgnoreCase))
                {
                    if (session.InPrivateChat)
                    {
                        ConsoleLogger.Error("You must exit current private chat first (/exitpm).");
                        continue;
                    }
                    string target = input.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase)
                        ? input[4..].Trim()
                        : input[3..].Trim();

                    if (string.IsNullOrWhiteSpace(target))
                    {
                        ConsoleLogger.Error("Usage: /pm <DisplayName>");
                        continue;
                    }

                    // Gửi yêu cầu vào PM, KHÔNG vào ngay
                    await _chat.SendMessageAsync(MessageBuilder.EnterPrivateRoom(target));

                    // Gán tạm nhưng không bật InPrivateChat
                    session.PrivateChatTarget = target;

                    ConsoleLogger.Info($"Requesting private chat with {target}...");
                    continue;
                }


                // USERS
                if (input.Equals("users", StringComparison.OrdinalIgnoreCase) ||
                   input.Equals("/users", StringComparison.OrdinalIgnoreCase))
                {
                    if (session.InPrivateChat)
                    {
                        ConsoleLogger.Error("Cannot use /users inside private chat.");
                        continue;
                    }
                    await _chat.SendMessageAsync(MessageBuilder.Users());
                    continue;
                }

                // HELP
                if (input.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                   input.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    if (session.InPrivateChat)
                    {
                        ConsoleLogger.Error("Cannot use /msg inside private chat.");
                        continue;
                    }
                    await _chat.SendMessageAsync(MessageBuilder.Help());
                    continue;
                }


                // PUBLIC MESSAGE
                if (input.StartsWith("msg ", StringComparison.OrdinalIgnoreCase) ||
                   input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
                {
                    if (session.InPrivateChat)
                    {
                        ConsoleLogger.Error("Cannot use /msg inside private chat.");
                        continue;
                    }
                    // BỎ prefix: "/msg " = 5 ký tự, "msg " = 4 ký tự
                    string content = input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase)
                        ? input[5..].Trim()
                        : input[4..].Trim();

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        ConsoleLogger.Error("Usage: msg <message>");
                        continue;
                    }

                    await _chat.SendMessageAsync($"MSG|{content}");
                    continue;
                }


                // UNKNOWN COMMAND
                ConsoleLogger.Error($"Unknown command: {input}. Type /help for commands.");
            }



            _chat.Disconnect();
        }
    }
}

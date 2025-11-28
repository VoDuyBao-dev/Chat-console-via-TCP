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
                    ConsoleLogger.Error("Disconnected from server.");
                    session.IsRunning = false;
                    return;
                }

                // Nếu CHƯA đăng nhập mà nhận [SERVER] ... thì coi đó là kết quả AUTH (sai username/password,...)
                if (!session.IsLoggedIn && message.StartsWith("[SERVER]"))
                {
                    // In thông báo lỗi từ server
                    ConsoleLogger.Info(message);
                    // Cho phép menu hiện lại
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

            // ConsoleLogger.Info("""

            //     === Chat Commands ===
            //     /help                - Show command list
            //     /users               - Show online users
            //     /pm <user> <msg>     - Private message
            //     exit                 - Leave room
            // """);


            // CHAT LOOP 
            // CHAT LOOP 
            while (true)
            {
                string? input = Console.ReadLine();
                if (input == null) continue;

                input = input.Trim();
                if (input == "") continue;

                // EXIT
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
                {
                    await _chat.SendMessageAsync("EXIT");
                    break;
                }

                // USERS
                if (input.Equals("users", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("/users", StringComparison.OrdinalIgnoreCase))
                {
                    await _chat.SendMessageAsync("USERS");
                    continue;
                }

                // HELP
                if (input.Equals("help", StringComparison.OrdinalIgnoreCase) ||
                    input.Equals("/help", StringComparison.OrdinalIgnoreCase))
                {
                    await _chat.SendMessageAsync("HELP");
                    continue;
                }

                // PRIVATE MESSAGE
                if (input.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase) ||
                    input.StartsWith("pm ", StringComparison.OrdinalIgnoreCase))
                {
                    // Bỏ prefix "/pm " hoặc "pm "
                    string content = input.StartsWith("/pm ", StringComparison.OrdinalIgnoreCase)
                        ? input[4..].Trim()
                        : input[3..].Trim();

                    // Tách ra 2 phần: username + message
                    var parts = content.Split(' ', 2);

                    if (parts.Length != 2)
                    {
                        ConsoleLogger.Error("Usage: /pm <DisplayName> <message>");
                        continue;
                    }

                    string target = parts[0];
                    string msg = parts[1];

                    ConsoleLogger.Error($"Sending private message to {target}: {msg}");

                    await _chat.SendMessageAsync($"PM|{target}|{msg}");
                    continue;
                }

                // PUBLIC MESSAGE
                if (input.StartsWith("msg ", StringComparison.OrdinalIgnoreCase) ||
                    input.StartsWith("/msg ", StringComparison.OrdinalIgnoreCase))
                {
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

                // CREATE GROUP
                if (input.StartsWith("creategroup", StringComparison.OrdinalIgnoreCase) ||
                    input.StartsWith("/creategroup", StringComparison.OrdinalIgnoreCase))
                {
                    // Lấy phần sau "/creategroup" và loại bỏ khoảng trắng thừa
                    string groupName = input.Length > "/creategroup".Length 
                    ? input.Substring("/creategroup".Length).Trim()
                    : "";

                    if (string.IsNullOrWhiteSpace(groupName))
                    {
                        ConsoleLogger.Error("Usage: creategroup <Group name>");
                        ConsoleLogger.Info("Ex: creategroup Group ABC");
                        continue;
                    }

                    if (groupName.Length > 50)
                    {
                        ConsoleLogger.Error("The group name cannot exceed 50 characters!");
                        continue;
                    }

                    if (groupName.Contains('|'))
                    {
                        ConsoleLogger.Error("The group name cannot contain the character '|'");
                        continue;
                    }

                    await _chat.SendMessageAsync(MessageBuilder.CreateGroup(groupName));
                    continue;
                }

                // INVITE TO GROUP - Hỗ trợ cả /invite và invite
                if (input.StartsWith("/invite", StringComparison.OrdinalIgnoreCase) ||
                    input.StartsWith("invite", StringComparison.OrdinalIgnoreCase))
                {   
                
                    string prefix = input.StartsWith("/invite", StringComparison.OrdinalIgnoreCase) ? "/invite" : "invite";

                    string rest = input.Substring(prefix.Length).TrimStart();

                    if (string.IsNullOrWhiteSpace(rest))
                    {
                        ConsoleLogger.Error("Usage: /invite <username> <group ID>");
                        ConsoleLogger.Info("Example: /invite Nam 5  or  invite Nam 5");
                        continue;
                    }

                    // Split username and groupId by the first space
                    int spaceIndex = rest.IndexOf(' ');
                    if (spaceIndex <= 0)
                    {
                        ConsoleLogger.Error("Missing GroupID! Usage: /invite <username> <ID>");
                        continue;
                    }

                    string username = rest.Substring(0, spaceIndex).Trim();
                    string idStr = rest.Substring(spaceIndex + 1).Trim();

                    if (string.IsNullOrWhiteSpace(username))
                    {
                        ConsoleLogger.Error("Username cannot be empty!");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(idStr) || !int.TryParse(idStr, out int groupId) || groupId <= 0)
                    {
                        ConsoleLogger.Error("GroupID must be a positive integer! Example: 5");
                        continue;
                    }

                    await _chat.SendMessageAsync(MessageBuilder.InviteToGroup(username, groupId));
                    continue;
                }

            // UNKNOWN COMMAND
            ConsoleLogger.Error($"Unknown command: {input}. Type /help for commands.");




            _chat.Disconnect();
        }
    }
    }
}

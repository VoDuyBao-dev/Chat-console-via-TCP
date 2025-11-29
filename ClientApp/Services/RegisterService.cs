using System;
using System.Threading.Tasks;
using ClientApp.Utilities;
using Common;

namespace ClientApp.Services
{
    public class RegisterService
    {
        private readonly TcpChatClient _chat;

        public RegisterService(TcpChatClient chat)
        {
            _chat = chat;
        }

        public async Task HandleRegisterAsync(bool maskPassword = false)
        {
            string username, password, display;

            // USERNAME
            while (true)
            {
                Console.Write("Username: ");
                username = Console.ReadLine()!.Trim();

                if (InputValidator.IsInvalid(username))
                {
                    ConsoleLogger.Error("Username cannot be empty.");
                    continue;
                }
                if (username.Length < 3)
                {
                    ConsoleLogger.Error("Username must be at least 3 characters.");
                    continue;
                }
                if (InputValidator.ContainsIllegalChars(username))
                {
                    ConsoleLogger.Error("Username contains illegal characters.");
                    continue;
                }
                break;
            }

            // PASSWORD
            while (true)
            {
                Console.Write("Password: ");

                if (maskPassword)
                    password = InputValidator.ReadPassword();
                else
                    password = Console.ReadLine()!;

                if (InputValidator.IsInvalid(password))
                {
                    ConsoleLogger.Error("Password cannot be empty.");
                    continue;
                }
                if (password.Length < 6)
                {
                    ConsoleLogger.Error("Password must be at least 6 characters.");
                    continue;
                }
                break;
            }

            // DISPLAY NAME
            while (true)
            {
                Console.Write("Display name: ");
                display = Console.ReadLine()!.Trim();

                if (InputValidator.IsInvalid(display))
                {
                    ConsoleLogger.Error("Display name cannot be empty.");
                    continue;
                }
                if (display.Length < 2)
                {
                    ConsoleLogger.Error("Display name must be at least 2 characters.");
                    continue;
                }
                if (InputValidator.ContainsIllegalChars(display))
                {
                    ConsoleLogger.Error("Display name contains illegal characters.");
                    continue;
                }
                break;
            }

            string passHash = Utils.PasswordHasher.SHA256Hash(password);

            // Client chỉ gửi request — server phản hồi
            await _chat.SendMessageAsync($"REGISTER|{username}|{passHash}|{display}\n");
        }
    }
}

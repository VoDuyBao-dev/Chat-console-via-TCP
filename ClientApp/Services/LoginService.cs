using System;
using System.Threading.Tasks;
using ClientApp.Utilities;
using Common;

namespace ClientApp.Services
{
    public class LoginService
    {
        private readonly TcpChatClient _chat;

        public LoginService(TcpChatClient chat)
        {
            _chat = chat;
        }

        public async Task HandleLoginAsync(bool maskPassword = false)
        {
            string username, password;

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
                break;
            }

            string passHash = Utils.PasswordHasher.SHA256Hash(password);

            await _chat.SendMessageAsync($"LOGIN|{username}|{passHash}");
        }
    }
}

using System;
using System.Threading.Tasks;
using ClientApp.Utilities;
using ClientApp.Services;
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

        public async Task HandleLoginAsync()
        {
            string u, p;

            while (true)
            {
                Console.Write("Username: ");
                u = Console.ReadLine()!.Trim();

                if (InputValidator.IsInvalid(u))
                {
                    ConsoleLogger.Error("Username không được bỏ trống.");
                    continue;
                }
                if (InputValidator.ContainsIllegalChars(u))
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

                if (InputValidator.IsInvalid(p))
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

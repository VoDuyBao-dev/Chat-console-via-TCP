using System;
using System.Threading.Tasks;
using ClientApp.Utilities;
using ClientApp.Services;
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

        public async Task HandleRegisterAsync()
        {
            string u, p, d;

            // Username
            while (true)
            {
                Console.Write("Username: ");
                u = Console.ReadLine()!.Trim();

                if (InputValidator.IsInvalid(u))
                {
                    ConsoleLogger.Error("Username không được bỏ trống.");
                    continue;
                }
                if (u.Length < 3)
                {
                    ConsoleLogger.Error("Username phải có ít nhất 3 ký tự.");
                    continue;
                }
                if (InputValidator.ContainsIllegalChars(u))
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

                if (InputValidator.IsInvalid(p))
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

                if (InputValidator.IsInvalid(d))
                {
                    ConsoleLogger.Error("Tên hiển thị không được bỏ trống.");
                    continue;
                }
                if (d.Length < 2)
                {
                    ConsoleLogger.Error("Tên hiển thị phải dài hơn 1 ký tự.");
                    continue;
                }
                if (InputValidator.ContainsIllegalChars(d))
                {
                    ConsoleLogger.Error("Tên hiển thị chứa ký tự không hợp lệ.");
                    continue;
                }
                break;
            }

            string passHash = Utils.PasswordHasher.SHA256Hash(p);
            await _chat.SendMessageAsync($"REGISTER|{u}|{passHash}|{d}");
        }
    }
}

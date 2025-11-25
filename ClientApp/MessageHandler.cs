using System;
using Common;

namespace ClientApp
{
    // internal class MessageHandler
    // {
    // }

    public static class MessageHandler
    {
        public static void Handle(string raw, UserSession session)
        {
            string[] parts = Protocol.Decode(raw);

            switch (parts[0])
            {
                case "REGISTER_OK":
                    Console.WriteLine("✓ Đăng ký thành công!");
                    break;

                case "REGISTER_FAIL":
                    Console.WriteLine("✗ Đăng ký thất bại: " + parts[1]);
                    break;

                case "LOGIN_OK":
                    Console.WriteLine("✓ Đăng nhập thành công!");
                    session.Login(parts[1]);  // Server nên gửi username
                    break;

                case "LOGIN_FAIL":
                    Console.WriteLine("✗ Sai tài khoản hoặc mật khẩu!");
                    break;

                default:
                    Console.WriteLine(raw); // Tin nhắn chat
                    break;
            }
        }
    }
}

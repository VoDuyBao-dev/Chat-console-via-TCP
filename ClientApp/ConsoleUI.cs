using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    // internal class ConsoleUI
    // {
    // }
    public static class ConsoleUI
    {
        public static void ShowWelcome()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("════════════════════════════════════════════");
            Console.WriteLine("     TCP Chat Client - Challenge Project    ");
            Console.WriteLine("════════════════════════════════════════════\n");
            Console.WriteLine("════════════════════════════════════");
            Console.WriteLine("     TCP CHAT CLIENT - LOGIN UI     ");
            Console.WriteLine("════════════════════════════════════\n");
            Console.ResetColor();
        }

        public static void ShowAuthMenu()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[1] Đăng ký tài khoản");
            Console.WriteLine("[2] Đăng nhập");
            Console.WriteLine("[0] Thoát");
            Console.Write("→ Chọn: ");
        }

        public static void ShowChatCommands()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("\n══════════════════════════");
            Console.WriteLine("  Bạn đã vào phòng chat!  ");
            Console.WriteLine("══════════════════════════");
            Console.WriteLine("Lệnh:");
            Console.WriteLine(" - Gửi cho tất cả: nhập nội dung và Enter");
            Console.WriteLine(" - Gửi riêng: /msg <username> <nội dung>");
            Console.WriteLine(" - Xem online: /list");
            Console.WriteLine(" - Thoát: exit\n");
        }
    }
}

using System;
using System.Runtime.CompilerServices;

namespace ClientApp
{
    internal class Program
    {
        private const string DefaultIP = "127.0.0.1";
        private const int DefaultPort = 5000;

        static void Main(string[] args)
        {
            Console.Title = "TCP Chat Client";

            // Lấy ip (nếu trong thì sử dụng mặc định)
            Console.Write($"Nhập IP của server (Mặc định:{DefaultIP}: ");
            string ipInput = Console.ReadLine();
            String ip = string.IsNullOrEmpty(ipInput) ? DefaultIP : ipInput;

            // Lấy Port và xử lý
            Console.Write($"Nhập port server (Mặc định: {DefaultPort}): ");
            string portInput = Console.ReadLine();
            int port;

            if (!int.TryParse(portInput, out port))
            {

                // Nhập sai hoặc để trống, sử dụng cổng mặc định
                port = DefaultPort;
                Console.WriteLine($"[CẢNH BÁO] Nhập sai cổng. Sử dụng cổng mặc định: {DefaultPort}");
            }

            ChatClient client = new ChatClient(ip, port);
            client.Connect();

            Console.ReadLine();
        }
    }
}

using System;

namespace ClientApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ConsoleUI.ShowWelcome();

            Console.Write("Nhập IP server (mặc định 127.0.0.1): ");
            string ip = Console.ReadLine() ?? "127.0.0.1";
            if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

            Console.Write("Nhập port (mặc định 5000): ");
            string portInput = Console.ReadLine() ?? "5000";
            int port = 5000;
            if (!int.TryParse(portInput, out port))
            {
                port = 5000;
            }


            ChatClient client = new ChatClient(ip, port);
            client.Start();

            Console.WriteLine("Client đã thoát.");
        }
    }
}

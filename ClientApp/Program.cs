
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace test_client_rieng
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Title = "TCP Chat Client - Challenge Project";
            Console.ForegroundColor = ConsoleColor.Yellow;


            Console.Write("Nhập username: ");
            string? username = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(username)) username = "Guest";

            Console.Write("Nhập IP server (mặc định 127.0.0.1): ");
            string? ipInput = Console.ReadLine();
            string ip = string.IsNullOrWhiteSpace(ipInput) ? "127.0.0.1" : ipInput.Trim();

            Console.Write("Nhập port (mặc định 5000): ");
            string? portInput = Console.ReadLine();
            int port = 5000;
            if (!int.TryParse(portInput, out port)) port = 5000;

            Console.Clear();
            Console.WriteLine("════════════════════════════════════");
            Console.WriteLine($" Đang kết nối tới {ip}:{port}...");
            Console.WriteLine($" Username: {username}");
            Console.WriteLine("════════════════════════════════════\n");

            try
            {
                TcpClient client = new TcpClient();
                await client.ConnectAsync(ip, port);

                NetworkStream stream = client.GetStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                // Gửi username
                await writer.WriteLineAsync(username);

                // Task nhận tin nhắn realtime
                _ = Task.Run(async () =>
                {
                    try
                    {
                        string? message;
                        while ((message = await reader.ReadLineAsync()) != null)
                        {
                            Console.WriteLine(message);
                        }
                    }
                    catch { }
                });

                Console.WriteLine("Đã kết nối thành công! Gõ tin nhắn và Enter để gửi.");
                Console.WriteLine("Gõ /quit để thoát.\n");

                // Vòng lặp gửi tin nhắn
                string? input;
                while ((input = Console.ReadLine()) != null)
                {
                    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    {
                        await writer.WriteLineAsync("exit");
                        break;
                    }
                    if (!string.IsNullOrWhiteSpace(input))
                        await writer.WriteLineAsync(input);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kết nối: {ex.Message}");
            }

            Console.WriteLine("\nĐã thoát. Nhấn phím bất kỳ để đóng...");
            Console.ReadKey();
        }
    }
}
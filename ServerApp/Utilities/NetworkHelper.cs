using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp.Utilities
{
    public class NetworkHelper
    {
        public static IPAddress? GetLocalIPv4()
        {
            try
            {
                // Tạo socket UDP
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);

                // không gửi dữ liệu thật, chỉ để hệ điều hành chọn interface đúng
                socket.Connect("8.8.8.8", 65530); 

                // Lấy địa chỉ IPv4 cục bộ đang dùng để ra Internet hoặc LAN
                return (socket.LocalEndPoint as IPEndPoint)?.Address;
            }
            catch
            {
                return null;
            }
        }

        // public static void PrintLocalIPs(int port)
        // {
        //     Console.WriteLine("\nIP to connect from another device in the LAN:");
        //     foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        //     {
        //         if (ip.AddressFamily == AddressFamily.InterNetwork)
        //             Console.WriteLine($" → {ip}:{port}");
        //     }
        //     Console.WriteLine();
        // }

        public static async Task<string?> SafeReadLineAsync(NetworkStream stream)
        {
            try
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
                return await reader.ReadLineAsync();
            }
            catch
            {
                return null; // client đã ngắt kết nối
            }
        }
    }
}

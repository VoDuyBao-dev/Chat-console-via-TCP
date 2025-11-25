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
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        public static void PrintLocalIPs(int port)
        {
            Console.WriteLine("\nIP to connect from another device in the LAN:");
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    Console.WriteLine($" → {ip}:{port}");
            }
            Console.WriteLine();
        }

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

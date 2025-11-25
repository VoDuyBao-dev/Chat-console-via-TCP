
using ClientApp;
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
            var client = new Client();
            await client.RunAsync();
        }
    }
}
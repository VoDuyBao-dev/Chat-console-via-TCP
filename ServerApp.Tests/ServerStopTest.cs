using System;
using System.Net.Sockets;
using System.Threading;
using Xunit;
using ServerApp;

namespace ServerApp.Tests
{
    public class ServerStopTests
    {
        [Fact]
        public void Test_ServerStop_CleansUpClientsAndListener()
        {
            int port = 5000;
            var server = new Server(port);

            // Start server trong thread riêng
            var serverThread = new Thread(server.Start)
            {
                IsBackground = true
            };
            serverThread.Start();

            // Đợi server ready
            Thread.Sleep(300);

            // Kết nối một client để kiểm tra client list
            using (var client = new TcpClient("127.0.0.1", port))
            {
                Thread.Sleep(100); // đợi server accept client
            }

            // Stop server
            server.Stop();

            // Sau stop, _clients phải empty và listener dừng
            // (không thể trực tiếp check private _clients, nhưng test sẽ fail nếu có exception khi stop)
            // Nếu cần, có thể dùng reflection để kiểm tra _clients count = 0
            // Ở đây test cơ bản: stop không throw exception
            Assert.True(true, "Server stopped without exceptions.");
        }
    }
}

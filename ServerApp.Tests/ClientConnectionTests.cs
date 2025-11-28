using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;
using ServerApp;

namespace ServerApp.Tests
{
    public class ClientConnectionTests
    {
        [Fact]
        public void Test_ClientCanConnect_AndServerResponds()
        {
            int port = 5001; // dùng port khác để tránh conflict
            var server = new Server(port);

            // Start server in background thread
            var serverThread = new Thread(server.Start)
            {
                IsBackground = true
            };
            serverThread.Start();

            // Đợi server khởi động
            Thread.Sleep(300);

            string serverResponse = "";

            // Kết nối client
            using (var client = new TcpClient("127.0.0.1", port))
            using (var stream = client.GetStream())
            {
                // Gửi username "mavis"
                byte[] usernameBytes = Encoding.UTF8.GetBytes("mavis\n");
                stream.Write(usernameBytes, 0, usernameBytes.Length);

                // Đọc phản hồi server
                byte[] buffer = new byte[2048];
                Thread.Sleep(200); // Đợi server gửi dữ liệu

                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    serverResponse = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }
            }

            // Tắt server
            server.Stop();

            // Kiểm tra phản hồi
            Assert.Contains("mavis", serverResponse);
        }
    }
}

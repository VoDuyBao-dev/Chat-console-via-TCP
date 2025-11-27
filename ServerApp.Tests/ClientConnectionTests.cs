using System;
using System.Net.Sockets;
using System.Threading;
using Xunit;
using ServerApp.Services;

namespace ServerApp.Tests
{
    public class ClientConnectionTests
    {
        [Fact]
        public void TcpListenerService_ShouldAcceptClient_AndStopSafely()
        {
            int port = 6000;
            bool clientConnected = false;

            // Khi có client kết nối → callback set true
            var listener = new TcpListenerService(port, client =>
            {
                clientConnected = true;
                client.Close(); // đóng ngay sau khi test
            });

            // Start server
            listener.Start();
            Thread.Sleep(200);

            // Tạo 1 client kết nối vào server
            using (var client = new TcpClient("127.0.0.1", port))
            {
                Thread.Sleep(200); // thời gian cho event callback
            }

            // Assert server nhận client
            Assert.True(clientConnected, "TcpListenerService should accept client connection.");

            // Stop → không được exception
            var ex = Record.Exception(() => listener.Stop());
            Assert.Null(ex);
        }
    }
}

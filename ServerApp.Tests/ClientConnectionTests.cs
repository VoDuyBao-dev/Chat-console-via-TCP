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
        public void TcpListener_ClientConnectionToServer()
        {
            int port = 6000;
            bool daNhanClient = false;

            Console.WriteLine("=== KIỂM THỬ KẾT NỐI CLIENT TCP ===");
            Console.WriteLine($"Khởi động TCP server tại cổng {port}...");

            var listener = new TcpListenerService(port, client =>
            {
                daNhanClient = true;
                Console.WriteLine("Server đã nhận kết nối từ một client.");
                client.Close();
            });

            listener.Start();
            Thread.Sleep(200);

            Console.WriteLine();
            Console.WriteLine("Server đang chạy và chờ client kết nối.");


            Console.WriteLine("Tiến hành tạo 1 client giả lập để kết nối tới server...");
            using (var client = new TcpClient("127.0.0.1", port))
            {
                Thread.Sleep(200); // thời gian để server callback xử lý
            }

            Assert.True(daNhanClient,
                "Test thất bại: server KHÔNG nhận kết nối từ client.");

            Console.WriteLine("Server đã nhận client thành công. (ĐÚNG)");

            Console.WriteLine();
            Console.WriteLine("Tiến hành dừng server...");
            var ex = Record.Exception(() => listener.Stop());
            Assert.Null(ex);

            Console.WriteLine("Server dừng thành công và không có lỗi xảy ra.");
            Console.WriteLine("=== KẾT THÚC TEST ===");
        }
    }
}

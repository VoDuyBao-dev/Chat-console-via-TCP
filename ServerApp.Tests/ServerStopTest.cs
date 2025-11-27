using System;
using System.Net.Sockets;
using System.Threading;
using Xunit;
using ServerApp.Services;

namespace ServerApp.Tests
{
    public class ServerStopTests
    {
        [Fact]
        public void Server_Stop_KiemTraKetNoiVaDungKhongLoi()
        {
            int port = 5055;
            bool daNhanKetNoiTruocKhiStop = false;

            Console.WriteLine("=== BẮT ĐẦU KIỂM THỬ SERVER STOP ===");
            Console.WriteLine("Khởi động TCP server...");

            var server = new TcpListenerService(port, client =>
            {
                daNhanKetNoiTruocKhiStop = true;
                Console.WriteLine("Server đã nhận kết nối từ client.");
                client.Close();
            });

            server.Start();
            Thread.Sleep(300); // đợi server lắng nghe cổng

            Console.WriteLine();
            Console.WriteLine("Server đang hoạt động.");
            Console.WriteLine("Cách kết nối thử:");
            Console.WriteLine($" - telnet 127.0.0.1 {port}");
            Console.WriteLine($" - sử dụng TcpClient(\"127.0.0.1\", {port})");
            Console.WriteLine();

            // Mô phỏng client kết nối
            Console.WriteLine("Tiến hành mô phỏng kết nối client tới server...");
            using (var client = new TcpClient("127.0.0.1", port))
            {
                Thread.Sleep(150);
            }

            Assert.True(daNhanKetNoiTruocKhiStop,
                "Server phải nhận được ít nhất 1 client trước khi dừng.");

            Console.WriteLine();
            Console.WriteLine("Dừng server...");
            var ex = Record.Exception(() => server.Stop());
            Assert.Null(ex);

            Console.WriteLine("Server đã dừng thành công, không phát sinh lỗi.");

            // Kiểm tra sau khi stop -> không được kết nối thêm
            bool ketNoiSauStop = false;

            try
            {
                using (var client = new TcpClient("127.0.0.1", port))
                {
                    ketNoiSauStop = true;
                }
            }
            catch
            {
                Console.WriteLine("Sau khi stop, client không thể kết nối nữa (đúng).");
            }

            Assert.False(ketNoiSauStop,
                "Sau khi Stop(), server phải từ chối kết nối mới.");

            Console.WriteLine();
            Console.WriteLine("=== KẾT THÚC TEST ===");
        }
    }
}

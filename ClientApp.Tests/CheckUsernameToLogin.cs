using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;

namespace ClientApp
{
    // =====================
    // Hàm kiểm tra username
    // =====================
    public class UserValidator
    {
        public static bool IsValidUsername(string username)
        {
            // Chỉ đúng "mavis" chính xác
            return username == "mavis";
        }
    }

    // =====================
    // Server TCP giả lập (dành cho test)
    // =====================
    public class MockServer
    {
        private TcpListener _listener;
        private Thread _serverThread;
        private bool _running;

        public int Port { get; }

        public MockServer(int port = 5000)
        {
            Port = port;
        }

        public void Start()
        {
            _running = true;
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();

            _serverThread = new Thread(() =>
            {
                while (_running)
                {
                    if (_listener.Pending())
                    {
                        TcpClient client = _listener.AcceptTcpClient();
                        NetworkStream stream = client.GetStream();

                        byte[] buffer = new byte[1024];
                        int read = stream.Read(buffer, 0, buffer.Length);
                        string username = Encoding.UTF8.GetString(buffer, 0, read);

                        // Gửi kết quả kiểm tra username
                        string response = UserValidator.IsValidUsername(username) ? "True" : "False";
                        byte[] respBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(respBytes, 0, respBytes.Length);

                        client.Close();
                    }
                    Thread.Sleep(10);
                }
            });
            _serverThread.Start();
        }

        public void Stop()
        {
            _running = false;
            _listener.Stop();
            _serverThread.Join();
        }
    }

    // =====================
    // Client helper (gửi username)
    // =====================
    public class ClientHelper
    {
        public static bool CheckUsername(string username, int port = 5000)
        {
            using TcpClient client = new TcpClient("127.0.0.1", port);
            NetworkStream stream = client.GetStream();

            byte[] bytes = Encoding.UTF8.GetBytes(username);
            stream.Write(bytes, 0, bytes.Length);

            byte[] buffer = new byte[1024];
            int read = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.UTF8.GetString(buffer, 0, read);

            return response == "True";
        }
    }

    // =====================
    // Test xUnit + Console log
    // =====================
    public class CheckUsernameLoginTests
    {
        private readonly int _port = 5000;

        [Theory]
        [InlineData("Mavis", false)]
        [InlineData("mAvis", false)]
        [InlineData("MAVIS", false)]
        [InlineData("maviss", false)]
        [InlineData("mavis", true)]
        public void CheckUsernameLogin_OutputTrueFalse(string input, bool expected)
        {
            // Start server
            MockServer server = new MockServer(_port);
            server.Start();

            // Client gửi username
            bool actual = ClientHelper.CheckUsername(input, _port);

            Console.WriteLine("=========================================");
            Console.WriteLine($"Kiểm tra username: \"{input}\"");
            Console.WriteLine($"Mong đợi       : {expected}");
            Console.WriteLine($"Thực tế        : {actual}");
            Console.WriteLine(actual == expected ? "KẾT QUẢ CHÍNH XÁC" : "KẾT QUẢ KHÔNG KHỚP — Test Fail");
            Console.WriteLine("=========================================");

            // Assert để xUnit nhận kết quả
            Assert.Equal(expected, actual);

            // Stop server
            server.Stop();
        }
    }
}

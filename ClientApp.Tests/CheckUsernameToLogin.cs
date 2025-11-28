using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Xunit;

namespace ClientApp.Tests
{
    // =======================
    // Database mô phỏng
    // =======================
    public static class SimpleDb
    {
        public static readonly Dictionary<string, string> Users = new()
        {
            { "mavis", "123456" } // chỉ đúng "mavis"
        };
    }

    // =======================
    // Mock TCP server
    // =======================
    public class DebugMockServer
    {
        private TcpListener _listener;
        private Thread _thread;
        private bool _running;
        public int Port { get; }

        public DebugMockServer(int port = 6000) => Port = port;

        public void Start()
        {
            _running = true;
            _listener = new TcpListener(IPAddress.Loopback, Port);
            _listener.Start();

            _thread = new Thread(() =>
            {
                while (_running)
                {
                    try
                    {
                        if (!_listener.Pending())
                        {
                            Thread.Sleep(10);
                            continue;
                        }

                        using TcpClient client = _listener.AcceptTcpClient();
                        using NetworkStream stream = client.GetStream();

                        byte[] buf = new byte[1024];
                        int read = stream.Read(buf, 0, buf.Length);
                        string raw = Encoding.UTF8.GetString(buf, 0, read).Trim();

                        var parts = raw.Split('|');
                        string usernameReceived = parts.Length > 0 ? parts[0] : "";
                        string passwordReceived = parts.Length > 1 ? parts[1] : "";

                        string response;

                        // Kiểm tra username case-sensitive
                        if (!SimpleDb.Users.ContainsKey(usernameReceived))
                        {
                            response = "[SERVER] Incorrect username";
                        }
                        else
                        {
                            string correctPassword = SimpleDb.Users[usernameReceived];
                            response = correctPassword == passwordReceived
                                ? "Login OK! Welcome to the chat room."
                                : "[SERVER] Incorrect password";
                        }

                        byte[] respBytes = Encoding.UTF8.GetBytes(response);
                        stream.Write(respBytes, 0, respBytes.Length);
                    }
                    catch { /* ignore for simplicity */ }
                }
            });

            _thread.Start();
        }

        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
            _thread.Join();
        }
    }

    // =======================
    // Client gửi username|password
    // =======================
    public static class DebugClient
    {
        public static string SendLogin(string username, string password, int port = 6000)
        {
            using TcpClient client = new TcpClient("127.0.0.1", port);
            using NetworkStream stream = client.GetStream();

            string msg = $"{username}|{password}\n";
            byte[] outb = Encoding.UTF8.GetBytes(msg);
            stream.Write(outb, 0, outb.Length);

            byte[] buf = new byte[1024];
            int read = stream.Read(buf, 0, buf.Length);
            return Encoding.UTF8.GetString(buf, 0, read).Trim();
        }
    }

    // =======================
    // xUnit test
    // =======================
    public class LoginTests
    {
        private readonly int _port = 6000;

        [Theory]
        // username đúng + password đúng
        [InlineData("mavis", "123456", "Login OK! Welcome to the chat room.")]
        // username đúng + password sai
        [InlineData("mavis", "wrongpass", "[SERVER] Incorrect password")]
        // username sai
        [InlineData("Mavis", "123456", "[SERVER] Incorrect username")]
        [InlineData("mAvis", "123456", "[SERVER] Incorrect username")]
        [InlineData("MAVIS", "123456", "[SERVER] Incorrect username")]
        [InlineData("unknown", "123456", "[SERVER] Incorrect username")]
        public void LoginTest_UsernamePassword(string username, string password, string expected)
        {
            var server = new DebugMockServer(_port);
            server.Start();

            string actual = DebugClient.SendLogin(username, password, _port);

            Console.WriteLine("=========================================");
            Console.WriteLine($"Kiểm tra đăng nhập: username=\"{username}\", password=\"{password}\"");
            Console.WriteLine($"Mong đợi đầu ra : {expected}");
            Console.WriteLine($"Thực tế đầu ra  : {actual}");
            Console.WriteLine(expected == actual ? "✔ KẾT QUẢ CHÍNH XÁC" : "❌ KHÔNG KHỚP — TEST FAIL");
            Console.WriteLine("=========================================");

            Assert.Equal(expected, actual);

            server.Stop();
        }
    }
}

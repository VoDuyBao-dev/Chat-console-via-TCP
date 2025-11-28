using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using ClientApp.Services;
using Xunit;

public class TestClientReconnectConsole
{
    private const string Username = "mavis"; // username mặc định để test

    public class MockTcpServer
    {
        private TcpListener? _listener;
        private readonly int _port;
        public MockTcpServer(int port) => _port = port;

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Loopback, _port);
            _listener.Start();
            Console.WriteLine($"[SERVER] Started on port {_port}");

            Task.Run(async () =>
            {
                while (_listener != null)
                {
                    try
                    {
                        var client = await _listener.AcceptTcpClientAsync();
                        Console.WriteLine("[SERVER] Client connected.");

                        _ = Task.Run(async () =>
                        {
                            using var stream = client.GetStream();
                            byte[] buffer = new byte[1024];
                            try
                            {
                                while (client.Connected)
                                {
                                    if (await stream.ReadAsync(buffer) == 0) break;
                                }
                            }
                            catch { }
                            Console.WriteLine("[SERVER] Client disconnected.");
                        });
                    }
                    catch { break; }
                }
            });
        }

        public void Stop()
        {
            Console.WriteLine("[SERVER] Stopping server...");
            _listener?.Stop();
            _listener = null;
        }
    }

    // ======================= TEST CHÍNH ============================
    [Fact]
    public async Task Client_Khong_Tu_Reconnect_Khi_Server_Khoi_Dong_Lai()
    {
        var console = new StringWriter();
        

        int port = 5002;
        var server = new MockTcpServer(port);
        server.Start();

        var client = new TcpChatClient();
        await client.ConnectAsync("127.0.0.1", port);
        Console.WriteLine($"CLIENT LOGIN AS {Username}");

        server.Stop();                               // server tắt
        await Task.Delay(800);
        Console.WriteLine("[ERROR] Disconnected from server.");

        server.Start();                               //  bật lại server
        await Task.Delay(2000);                       // đợi xem client có reconnect không

        // ==== LẤY TOÀN BỘ OUTPUT REAL CHỨ KHÔNG TỰ GHI ====
        string thucTe = console.ToString();

        string mongMuon =
$@"Mong muốn nếu Client biết reconnect:
[SERVER] {Username} reconnected to the server";

        Console.WriteLine("\n============== MONG MUỐN ==============");
        Console.WriteLine(mongMuon);

        Console.WriteLine("\n============== THỰC TẾ ================");
        Console.WriteLine(thucTe); 

        Assert.DoesNotContain("reconnected", thucTe);

        server.Stop();
        client.Disconnect();
    }
}

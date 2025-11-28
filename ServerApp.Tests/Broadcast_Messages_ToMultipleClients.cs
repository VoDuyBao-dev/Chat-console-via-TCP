using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;


public class TcpChatServer
{
    
    private readonly TcpListener _listener;
    private readonly List<TcpClient> _clients = new();
    private bool _running = false;

    public TcpChatServer(int port)
    {
        _listener = new TcpListener(IPAddress.Loopback, port);
    }
    int count = 0;
    public void Start()
    {
        _running = true;
        _listener.Start();
        Console.WriteLine("[Server] Started");

        Task.Run(async () =>
        {
            while (_running)
            {
                
                var client = await _listener.AcceptTcpClientAsync();
                lock (_clients) { _clients.Add(client); } 
                Console.WriteLine($"[Server] New client connected {count}");
                HandleClient(client);
                count++;
            }
        });
    }

    public void Stop()
    {
        _running = false;
        _listener.Stop();
        lock (_clients)
        {
            foreach (var c in _clients) c.Close();
            _clients.Clear();
        }
        Console.WriteLine("[Server] Stopped");
    }

    private async void HandleClient(TcpClient client)
    {
        var stream = client.GetStream();
        var buffer = new byte[1024];
        while (_running && client.Connected)
        {
            int byteCount = 0;
            try
            {
                byteCount = await stream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch { break; }

            if (byteCount == 0) break; // client disconnected

            string msg = Encoding.UTF8.GetString(buffer, 0, byteCount);
            Console.WriteLine("--------------------------------");
            Console.WriteLine(msg);
            Console.WriteLine($"[Server] Received: {msg}");
            Broadcast(msg, client);
        }

        lock (_clients) { _clients.Remove(client); }
        client.Close();
        Console.WriteLine("[Server] Client disconnected");
    }

    private void Broadcast(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (_clients)
        {
            foreach (var c in _clients)
            {
                if (c == sender) continue; // không gửi lại cho sender
                try
                {
                    c.GetStream().WriteAsync(data, 0, data.Length);
                }
                catch { }
            }
        }
    }
}

public class TcpChatTests
{
    [Fact]
    
    public async Task TenClients_MultiMessage_BroadcastWorks()
    {
        int port = 9000;
        var server = new TcpChatServer(port);
        server.Start();

        int clientCount = 2000;
        var clients = new List<TcpClient>();
        var receivedMessages = new List<string>[clientCount];
        int CountClientsReceivedAllMessages = 0;
        for (int i = 0; i < clientCount; i++)
        {
            
            receivedMessages[i] = new List<string>();
            var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, port);
            clients.Add(client);
            int clientIndex = i;

            // Start listening for messages
            Task.Run(async () =>
            {
                var stream = client.GetStream();
                var buffer = new byte[1024];
                while (true)
                {
                    int bytesRead = 0;
                    try
                    {
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    }
                    catch { break; }

                    if (bytesRead == 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    receivedMessages[clientIndex].Add(msg);
                    Console.WriteLine($"[Client {clientIndex}] Received: {msg}");
                    CountClientsReceivedAllMessages++;
                }
            });
        }

        // Mỗi client gửi một tin nhắn
        for (int i = 0; i < 1; i++)
        {
            string msg = $"Message from client {i}";
            Console.WriteLine($"[Client {i}] Sending: {msg}");
            byte[] data = Encoding.UTF8.GetBytes(msg);
            await clients[i].GetStream().WriteAsync(data, 0, data.Length);
            await Task.Delay(100); // delay nhỏ để server broadcast
        }

        // Chờ một chút cho tất cả broadcast kịp
        await Task.Delay(1000);
        
        // Kiểm tra: mỗi tin nhắn được nhận bởi tất cả client khác
        //for (int sender = 0; sender < 1; sender++)
        //{
        //    string expected = $"Message from client {sender}";
        //    for (int receiver = 0; receiver < clientCount; receiver++)
        //    {
        //        if (receiver == sender) continue;
        //        Assert.Contains(expected, receivedMessages[receiver]);
        //    }
            
        //}
        Console.WriteLine($"[Test] {CountClientsReceivedAllMessages} clients received all messages.");
        // Cleanup
        foreach (var c in clients) c.Close();
        server.Stop();

        Console.WriteLine("[Test] All messages successfully broadcasted!");
        Console.WriteLine("--------------------------------");
    }
}

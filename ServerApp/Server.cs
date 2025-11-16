using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ServerApp
{
    public class Server
    {
        private TcpListener _listener;

        // Bắt đầu server
        // test ở đây xem có bị looi khong
        public void Start(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Console.WriteLine($" Server started. Listening on port {port}...");
                Console.WriteLine(" Waiting for client connections...\n");

                while (true)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Console.WriteLine($" Client connected from {client.Client.RemoteEndPoint}");

                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error: {ex.Message}");
            }
        }

        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int byteCount;

            try
            {
                while (true)
                {
                    byteCount = stream.Read(buffer, 0, buffer.Length);

                    // ====== (1) Client ngắt kết nối chủ động ======
                    if (ConnectionHandler.IsClientDisconnect(byteCount))
                    {
                        Console.WriteLine($" Client {client.Client.RemoteEndPoint} disconnected normally.");
                        break;
                    }

                    // ====== (2) Message quá lớn (đầy buffer) ======
                    if (ConnectionHandler.IsTooLarge(byteCount, buffer.Length))
                    {
                        Console.WriteLine(" Warning: message too large. Possible overflow or long message.");
                    }

                    // ====== (3) Giải mã UTF-8 an toàn ======
                    if (!ConnectionHandler.TryDecode(buffer, byteCount, out string message))
                    {
                        Console.WriteLine(" Invalid data received. UTF-8 decode failed.");
                        continue; // bỏ qua gói lỗi, tiếp tục đọc
                    }

                    // ====== (4) Lệnh QUIT ======
                    if (ConnectionHandler.IsQuitCommand(message))
                    {
                        Console.WriteLine($" Client {client.Client.RemoteEndPoint} requested QUIT.");
                        break;
                    }

                    // ====== (5) Message trống ======
                    if (ConnectionHandler.IsEmpty(message))
                    {
                        Console.WriteLine(" Empty message ignored.");
                        continue;
                    }

                    Console.WriteLine($" Message from {client.Client.RemoteEndPoint}: {message}");

                    // ====== (6) Echo lại client giữ nguyên logic cũ ======
                    string response = $"Server received: {message}";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Client {client.Client.RemoteEndPoint} disconnected with error: {ex.Message}");
            }
            finally
            {
                stream.Close();   
                client.Close();
            }
        }
    }
}

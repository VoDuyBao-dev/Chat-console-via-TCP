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
        public void Start(int port)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, port);
                _listener.Start();
                Console.WriteLine($" Server started. Listening on port {port}...");
                Console.WriteLine(" Waiting for client connections...\n");

                // Vòng lặp chờ client kết nối
                while (true)
                {
                    TcpClient client = _listener.AcceptTcpClient();
                    Console.WriteLine($" Client connected from {client.Client.RemoteEndPoint}");

                    // Tạo luồng riêng cho mỗi client
                    Thread clientThread = new Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Error: {ex.Message}");
            }
        }

        // Hàm xử lý client
        private void HandleClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int byteCount;

            try
            {
                while ((byteCount = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, byteCount);
                    Console.WriteLine($" Message from {client.Client.RemoteEndPoint}: {message}");

                    // Gửi phản hồi lại client (echo)
                    string response = $"Server received: {message}";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Client {client.Client.RemoteEndPoint} disconnected: {ex.Message}");
            }
            finally
            {
                client.Close();
            }
        }
    }
}

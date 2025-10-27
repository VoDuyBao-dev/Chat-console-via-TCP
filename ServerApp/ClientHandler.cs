using System;
using System.Net.Sockets;
using System.Text;

namespace ServerApp
{
    public class ClientHandler
    {
        private TcpClient client;
        private NetworkStream stream;

        public ClientHandler(TcpClient client)
        {
            this.client = client;
            this.stream = client.GetStream();
        }

        public void HandleClient()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[CLIENT]: {message}");

                    // Gửi lại phản hồi
                    string response = $"Server đã nhận: {message}";
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CLIENT HANDLER] Lỗi: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                client?.Close();
                Console.WriteLine("[SERVER] Client ngắt kết nối.");
            }
        }
    }
}

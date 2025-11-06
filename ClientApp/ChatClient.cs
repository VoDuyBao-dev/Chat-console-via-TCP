using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Threading;


namespace ClientApp
{
    public class ChatClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private string _serverIP;
        private int _serverPort;
        private bool _isRunning = true; // kiểm soát trạng thái

        public ChatClient(string ip, int port)
        {
            _serverIP = ip;
            _serverPort = port;
        }

        // Kết nối tới server 
        public void Connect()
        {
            try
            {
                _client = new TcpClient();
                _client.Connect(_serverIP, _serverPort);
                Console.WriteLine($"\n Connected to server {_serverIP}:{_serverPort}");

                _stream = _client.GetStream();

                // Taọ luồng nhận tin nhắn song song
                Thread receiveThread = new Thread(ReceiveMessages);
                receiveThread.Start();

                // Cho phép người dùng nhập tin nhắn gửi 
                SendMessages();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n Connection error: {ex.Message}");
            }
            finally
            {
                Disconnect();
            }

        }

        // Nhận tin nhắn từ server 
        private void ReceiveMessages()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (_isRunning && (bytesRead = _stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"\n Server: {message}");
                    Console.Write("You: ");
                }
            }
            catch (Exception)
            {
                if (_isRunning)
                {
                    Console.WriteLine("\n Disconnected from server.");
                }
            }
            finally
            {
                _isRunning = false; // Luồng gửi cũng dừng lại nếu luồng nhận lỗi 
            }
        }

        // Gửi tin nhắn lên server 
        private void SendMessages()
        {
            Console.WriteLine("Type message and press Enter (type 'exit' to quit)\n");

            while (_isRunning)
            {
                Console.Write("You: ");
                string msg = Console.ReadLine();

                if (msg?.ToLower() == "exit")
                {
                    _isRunning = false;
                    break;
                }
                if (!string.IsNullOrEmpty(msg))
                {
                    try
                    {
                        byte[] data = Encoding.UTF8.GetBytes(msg);
                        _stream.Write(data, 0, data.Length);
                        _stream.Flush();
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("\n[LỖI GỬI] Mất kết nối. Vui lòng nhấn Enter để thoát.");
                        _isRunning = false;
                        break;
                    }
                }
            }
        }
        private void Disconnect()
        {
            _isRunning = false;
            _stream?.Close();
            _client?.Close();
            Console.WriteLine(" Disconnected from server. Press Enter to close...");
        }
    }
 }

// ClientApp/Services/TcpChatClient.cs
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using ClientApp.Utilities;

namespace ClientApp.Services
{
    public class TcpChatClient : IDisposable
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private StreamWriter? _writer;
        private StreamReader? _reader;

        // Callback để thông báo có tin nhắn mới (chỉ có 1 luồng đọc!)
        private Action<string>? _onMessageReceived;

        public bool IsConnected => _client?.Connected == true;

        public async Task ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();

            _writer = new StreamWriter(_stream, Encoding.UTF8)
            {
                AutoFlush = true,
                NewLine = "\n"
            };

            _reader = new StreamReader(_stream, Encoding.UTF8);

            ConsoleLogger.Success($"Đã kết nối tới {ip}:{port}");
        }

        public async Task SendMessageAsync(string message)
        {
            if (_writer == null || !IsConnected) return;

            try
            {
                await _writer.WriteLineAsync(message.Trim());
            }
            catch (Exception ex)
            {
                ConsoleLogger.Error($"Gửi thất bại: {ex.Message}");
            }
        }

       
        /// Bắt đầu nhận tin nhắn - CHỈ GỌI 1 LẦN DUY NHẤT!
       
        public void StartReceiving(Action<string> onMessageReceived)
        {
            if (_onMessageReceived != null)
                throw new InvalidOperationException("StartReceiving đã được gọi trước đó!");

            _onMessageReceived = onMessageReceived;

            // Chạy nền, không await ở đây
            Task.Run(async () =>
            {
                try
                {
                    while (_reader != null && IsConnected)
                    {
                        string? line = await _reader.ReadLineAsync();
                        if (line == null)
                        {
                            _onMessageReceived?.Invoke("[SERVER] Mất kết nối với server.");
                            break;
                        }

                        // Đẩy mọi tin nhắn lên trên để Client xử lý
                        _onMessageReceived?.Invoke(line);
                    }
                }
                catch (Exception ex)
                {
                    _onMessageReceived?.Invoke($"[LỖI] {ex.Message}");
                }
                finally
                {
                    _onMessageReceived?.Invoke("[DISCONNECTED]");
                    Disconnect();
                }
            });
        }

        public void Disconnect()
        {
            try
            {
                _writer?.Close();
                _reader?.Close();
                _stream?.Close();
                _client?.Close();
            }
            catch { /* ignore */ }
            finally
            {
                _writer = null;
                _reader = null;
                _stream = null;
                _client = null;
            }
        }

        public void Dispose() => Disconnect();
    }
}
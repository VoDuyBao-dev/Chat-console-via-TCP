using ServerApp.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ServerApp.Utilities;

namespace ServerApp.Services
{
    public class UdpDiscoveryService
    {
        private readonly int _tcpPort;
        private readonly int _udpPort;
        private bool _running;

        public UdpDiscoveryService(int tcpPort, int udpPort)
        {
            _tcpPort = tcpPort;
            _udpPort = udpPort;
        }

        public void Start()
        {
            _running = true;
            Task.Run(BroadcastLoop);
            Task.Run(ListenForDiscoveryRequests);
        }

        public void Stop() => _running = false;

        private async Task BroadcastLoop()
        {
            using var udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            var localIp = NetworkHelper.GetLocalIPv4();
            if (localIp == null)
            {
                ConsoleLogger.Error("Not found IP IPv4 to broadcast!");
                return;
            }

            var message = $"ChatServer|{localIp}|{_tcpPort}|{AppSettings.Current.ServerName}";
            var data = Encoding.UTF8.GetBytes(message);

            while (_running)
            {
                try
                {
                    var endpoint = new IPEndPoint(IPAddress.Broadcast, _udpPort);
                   
                    await udpClient.SendAsync(data, data.Length, endpoint);
                   // ConsoleLogger.Broadcast($"sent discovery: {message}");
                }
                catch (Exception ex)
                {
                    ConsoleLogger.Error($"UDP Error: {ex.Message}");
                }

                await Task.Delay(AppSettings.Current.BroadcastIntervalMs);
            }
        }

        private async Task ListenForDiscoveryRequests()
        {
            using var listener = new UdpClient(_udpPort); // lắng nghe trên port 5001

            var localIp = NetworkHelper.GetLocalIPv4();
            if (localIp == null) return;

            var responseMessage = $"ChatServer|{localIp}|{_tcpPort}|{AppSettings.Current.ServerName}";
            var responseData = Encoding.UTF8.GetBytes(responseMessage);

            ConsoleLogger.Info("UDP Discovery Listener started – ready to reply to DISCOVER requests");

            while (_running)
            {
                try
                {
                    var result = await listener.ReceiveAsync();
                    string request = Encoding.UTF8.GetString(result.Buffer).Trim();

                    // ← ĐÚNG CHỖ NÀY: KHI NHẬN ĐƯỢC "DISCOVER" → TRẢ LỜI NGAY!
                    if (request == "DISCOVER")
                    {
                        await listener.SendAsync(responseData, responseData.Length, result.RemoteEndPoint);
                        ConsoleLogger.Success($"Replied to DISCOVER from {result.RemoteEndPoint}");
                    }
                }
                catch (Exception ex) when (!_running)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ConsoleLogger.Error($"Discovery listener error: {ex.Message}");
                }
            }
        }
    }
}

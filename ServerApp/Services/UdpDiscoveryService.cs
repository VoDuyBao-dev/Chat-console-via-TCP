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
                    ConsoleLogger.Broadcast($"sent discovery: {message}");
                }
                catch (Exception ex)
                {
                    ConsoleLogger.Error($"UDP Error: {ex.Message}");
                }

                await Task.Delay(AppSettings.Current.BroadcastIntervalMs);
            }
        }
    }
}

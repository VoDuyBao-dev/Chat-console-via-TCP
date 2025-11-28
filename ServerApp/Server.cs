using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ServerApp.Config;
using ServerApp.Models;
using ServerApp.Services;
using ServerApp.Utilities;

namespace ServerApp
{
    public class Server
    {
        private readonly TcpListenerService _tcpService;
        private readonly UdpDiscoveryService _udpService;

        public Server()
        {
            var settings = AppSettings.Current;
            var clientConnections = new ClientConnectionService();

            _tcpService = new TcpListenerService(settings.TcpPort, clientConnections.HandleNewClient);
            _udpService = new UdpDiscoveryService(settings.TcpPort, settings.UdpPort);
        }

        public void Start()
        {
            _udpService.Start();
            _tcpService.Start();
            ConsoleLogger.Success($"Server running on TCP {AppSettings.Current.TcpPort} | UDP Discovery {AppSettings.Current.UdpPort}");
            ConsoleLogger.Info($"Server is sending broadcast");
        }

        public void Stop()
        {
            _tcpService.Stop();
            _udpService.Stop();
            ConsoleLogger.Info("Server stopped.");
        }



    }
}
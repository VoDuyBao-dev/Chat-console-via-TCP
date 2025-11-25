using ClientApp.Models;
using ClientApp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp.Services
{
    public class UdpDiscoveryClient
    {
        private const int DiscoveryPort = 5001;
        private readonly List<ServerInfo> _discoveredServers = new();
        private bool _listening = true;

        public async Task<List<ServerInfo>> DiscoverServersAsync(TimeSpan timeout)
        {
            _discoveredServers.Clear();
            _listening = true;

            ConsoleLogger.Info("Đang tìm server trong mạng LAN (UDP port 5001)...");

            using var udpClient = new UdpClient(DiscoveryPort);
            var cts = new CancellationTokenSource(timeout);

            Task listenTask = Task.Run(async () =>
            {
                while (_listening && !cts.IsCancellationRequested)
                {
                    try
                    {
                        var result = await udpClient.ReceiveAsync(cts.Token);
                        var message = Encoding.UTF8.GetString(result.Buffer);

                        if (message.StartsWith("ChatServer|"))
                        {
                            var parts = message.Split('|');
                            if (parts.Length >= 4)
                            {
                                var server = new ServerInfo
                                {
                                    Name = parts[3],
                                    Ip = parts[1],
                                    Port = int.Parse(parts[2])
                                };

                                if (!_discoveredServers.Any(s => s.Ip == server.Ip))
                                {
                                    _discoveredServers.Add(server);
                                    ConsoleLogger.Success($"Phát hiện: {server}");
                                }
                            }
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch { /* ignore */ }
                }
            });

            await Task.Delay(timeout);
            _listening = false;
            await listenTask;

            return _discoveredServers;
        }
    }
}

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
    public class TcpListenerService
    {
        private readonly int _port;
        private readonly Action<TcpClient> _onClientConnected;
        private TcpListener? _listener;
        private bool _running;

        public TcpListenerService(int port, Action<TcpClient> onClientConnected)
        {
            _port = port;
            _onClientConnected = onClientConnected;
        }

        public void Start()
        {
            _running = true;
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();

            // NetworkHelper.PrintLocalIPs(_port);

            Task.Run(AcceptLoop);
        }

        public void Stop()
        {
            _running = false;
            _listener?.Stop();
        }

        private async Task AcceptLoop()
        {
            while (_running)
            {
                try
                {
                    var client = await _listener!.AcceptTcpClientAsync();
                    _onClientConnected(client);
                }
                catch when (!_running) { break; }
                catch (Exception ex)
                {
                    Console.WriteLine($"Accept Error: {ex.Message}");
                }
            }
        }
    }
}

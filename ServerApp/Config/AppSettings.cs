using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp.Config
{
    public class AppSettings
    {
        public static AppSettings Current { get; } = Load();

        public int TcpPort { get; set; } = 5000;
        public int UdpPort { get; set; } = 5001;
        public int BroadcastIntervalMs { get; set; } = 3000;
        public string ServerName { get; set; } = "Chat console via TCP Server";

        private static AppSettings Load()
        {
                 return new AppSettings();
        }
    }
}

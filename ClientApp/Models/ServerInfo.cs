using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp.Models
{
    public class ServerInfo
    {
        public string Name { get; set; } = "";
        public string Ip { get; set; } = "";
        public int Port { get; set; }
        public DateTime DiscoveredAt { get; set; } = DateTime.Now;

        public override string ToString() => $"{Name} ({Ip}:{Port}) - {DiscoveredAt:HH:mm:ss}";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;

namespace ServerApp.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string? DisplayName { get; set; }
        public User? PrivateChatTarget { get; set; }
        public bool InPrivateChat { get; set; }
        public int? PrivateChatTargetId { get; set; }   // user offline ID
        public string? PrivateChatTargetName { get; set; }


        public string Username { get; set; }
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get; set; }
        public StreamReader Reader { get; set; }
        public StreamWriter Writer { get; set; }
        public Thread Thread { get; set; }

        public User(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
            Reader = new StreamReader(Stream);
            Writer = new StreamWriter(Stream, new UTF8Encoding(false)) { AutoFlush = true };
        }
        public User()
        {
            // offline user — không có stream, không crash
        }



        public override string ToString()
        {
            return Username ?? Client.Client.RemoteEndPoint.ToString();
        }
    }
}

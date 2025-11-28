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
<<<<<<< HEAD
        public int UserId { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastLogout { get; set; }

        public TcpClient Client { get; set; } // ket noi dang dung

        public User() { }

        public User(string username, TcpClient client)
        {
            Username = username;
            Client = client;
            IsOnline = true;
=======
        public int UserId { get; set; } = 0; // Guest = 0
        public string? DisplayName { get; set; }

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

        public override string ToString()
        {
            return Username ?? Client.Client.RemoteEndPoint.ToString();
>>>>>>> fdeed3a93b879a02bf8134ae80c14bf4eba935c5
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp.Models
{
    public class User
    {
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
        }
    }
}

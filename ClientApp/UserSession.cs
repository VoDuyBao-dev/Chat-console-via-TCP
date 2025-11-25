using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp
{
    // internal class UserSession
    // {
    // }
    public class UserSession
    {
        public bool IsLoggedIn { get; set; } = false;
        public string? Username { get; set; }
        public bool IsRunning { get; set; } = true;

        public void Login(string username)
        {
            Username = username;
            IsLoggedIn = true;
        }

        public void Logout()
        {
            Username = null;
            IsLoggedIn = false;
            IsRunning = false;
        }
    }
}

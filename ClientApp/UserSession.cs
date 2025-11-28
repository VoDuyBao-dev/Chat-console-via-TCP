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

        // Cờ: đang chờ server trả lời LOGIN / REGISTER
        public bool IsWaitingAuth { get; set; }

        public void Login(string username)
        {
            Username = username;
            IsWaitingAuth = false;
            IsLoggedIn = true;
        }


        public void ResetAuthWait()
        {
            IsWaitingAuth = false;
        }

        
        public void Logout()
        {
            Username = null;
            IsLoggedIn = false;
            IsRunning = false;
        }
    }
}

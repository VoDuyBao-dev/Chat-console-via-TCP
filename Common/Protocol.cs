using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// namespace Common
// {
//     internal class Protocol
//     {
//     }
    
// }

namespace Common
{
    public static class Protocol
    {
        // Tạo gói đăng ký gửi lên server
        public static string BuildRegisterPacket(string username, string password)
        {
            return $"REGISTER|{username}|{password}";
        }

        // Phân tích gói tin từ client gửi đến server
        public static string[] Decode(string raw)
        {
            return raw.Split('|');
        }
    }
}

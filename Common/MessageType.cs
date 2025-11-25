using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    // internal class MessageType
    // {
    // }
    public enum MessageType
    {
        Private = 0,   // Gửi đến 1 người
        Broadcast = 1, // Gửi cho tất cả
        System = 2     // Tin nhắn hệ thống
    }

}

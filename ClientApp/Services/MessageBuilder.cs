using Common;

namespace ClientApp.Services
{
    public static class MessageBuilder
    {
        public static string Register(string username, string passHash, string display)
        {
            return $"{Protocol.REGISTER}{Protocol.Split}{username}{Protocol.Split}{passHash}{Protocol.Split}{display}";
        }

        public static string Login(string username, string passHash)
        {
            return $"{Protocol.LOGIN}{Protocol.Split}{username}{Protocol.Split}{passHash}";
        }

        // public static string Guest()
        // {
        //     return Protocol.GUEST;
        // }

        public static string PublicMessage(string msg)
        {
            return $"{Protocol.MSG}{Protocol.Split}{msg}";
        }

        // public static string PrivateMessage(string target, string msg)
        // {
        //     return $"{Protocol.PM}{Protocol.Split}{target}{Protocol.Split}{msg}";
        // }

        // chat group
        // create group
        public static string CreateGroup(string groupName)
        {
            return $"{Protocol.CREATEGROUP}{Protocol.Split}{groupName}";
        }

        // invite to group
        public static string InviteToGroup(string username, int groupId)
        {
            return $"{Protocol.INVITE}{Protocol.Split}{username}{Protocol.Split}{groupId}";
        }

// group message
        public static string JoinGroup(int groupId)
        {
           return $"{Protocol.JOINGROUP}{Protocol.Split}{groupId}";
        }
            

        public static string LeaveGroup()
        {
            return Protocol.LEAVEGROUP;
            
        }
        

        public static string MyGroups()
        {
            return Protocol.MYGROUPS;
        }   



        public static string Help()
        {
            return Protocol.HELP;
        }

        public static string Users()
        {
            return Protocol.USERS;
        }

        public static string Exit()
        {
            return Protocol.EXIT;
        }
         // Vào phòng chat riêng
        public static string EnterPrivateRoom(string targetDisplayName)
            => $"{Protocol.ENTER_PM}{Protocol.Split}{targetDisplayName}";

        // Gửi tin nhắn riêng khi đã vào phòng
        public static string PrivateMessage(string msg)
            => $"{Protocol.PRIVMSG}{Protocol.Split}{msg}";

        // Thoát phòng chat riêng
        public static string ExitPrivateRoom()
            => $"{Protocol.EXIT_PM}";

    }
}

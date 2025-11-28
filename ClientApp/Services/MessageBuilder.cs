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

        public static string PrivateMessage(string target, string msg)
        {
            return $"{Protocol.PM}{Protocol.Split}{target}{Protocol.Split}{msg}";
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
    }
}

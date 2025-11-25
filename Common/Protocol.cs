namespace Common
{
    public static class Protocol
    {
        // ====== CLIENT → SERVER ======

        public static string BuildRegister(string username, string password)
        {
            return $"REGISTER|{username}|{password}";
        }

        public static string BuildLogin(string username, string password)
        {
            return $"LOGIN|{username}|{password}";
        }

        public static string BuildBroadcast(string content)
        {
            return $"ALL|{content}";
        }

        public static string BuildPrivate(string toUser, string content)
        {
            return $"MSG|{toUser}|{content}";
        }

        // ====== SERVER → CLIENT ======

        public const string REGISTER_OK = "REGISTER_OK";
        public const string REGISTER_FAIL = "REGISTER_FAIL";

        public const string LOGIN_OK = "LOGIN_OK";
        public const string LOGIN_FAIL = "LOGIN_FAIL";

        // ====== DECODER ======
        public static string[] Decode(string raw)
        {
            return raw.Split('|');
        }
    }
}

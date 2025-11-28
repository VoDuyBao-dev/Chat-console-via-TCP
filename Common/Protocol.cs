namespace Common
{
    public static class Protocol
    {
        public const char Split = '|';

        // AUTH
        public const string REGISTER = "REGISTER";
        public const string LOGIN = "LOGIN";
        // public const string GUEST = "GUEST";

        // CHAT
        public const string MSG = "MSG";
        public const string PM = "PM";
       
       // GROUP CHAT
       public const string CREATEGROUP = "CREATEGROUP";
       public const string INVITE     = "INVITE";
       public const string GROUPMSG   = "GROUPMSG";

        // SYSTEM
        public const string USERS = "USERS";
        public const string HELP = "HELP";
        public const string PING = "PING";
        public const string EXIT = "EXIT";

        // SERVER RESPONSES
        public const string REGISTER_SUCCESS = "REGISTER_SUCCESS";
        public const string LOGIN_SUCCESS = "LOGIN_SUCCESS";

        public const string ERROR = "ERROR";
    }
}

namespace ClientApp.Services
{
    public static class MessageBuilder
    {
        public static string BuildPrivateMessage(string to, string msg)
            => $"/pm|{to}|{msg}";
    }
}

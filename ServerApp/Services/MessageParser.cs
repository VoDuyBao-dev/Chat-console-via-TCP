using Common;

namespace ServerApp.Services
{
    public class ParsedMessage
    {
        public string Command { get; set; } = "";
        public string[] Args { get; set; } = Array.Empty<string>();
    }

    public static class MessageParser
    {
        public static ParsedMessage Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new ParsedMessage();

            var split = raw.Split(Protocol.Split);

            return new ParsedMessage
            {
                Command = split[0].Trim().ToUpper(),
                Args = split.Skip(1).ToArray()
            };
        }
    }
}

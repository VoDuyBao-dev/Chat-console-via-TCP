using System;
using System.Text;

namespace ServerApp
{
    public static class ConnectionHandler
    {
        public static bool IsClientDisconnect(int byteCount)
            => byteCount == 0;

        public static bool IsTooLarge(int byteCount, int bufferSize)
            => byteCount == bufferSize;

        public static bool IsQuitCommand(string message)
            => message.Trim().Equals("QUIT", StringComparison.OrdinalIgnoreCase);

        public static bool IsEmpty(string message)
            => string.IsNullOrWhiteSpace(message);

        public static bool TryDecode(byte[] buffer, int count, out string message)
        {
            try
            {
                message = Encoding.UTF8.GetString(buffer, 0, count);
                return true;
            }
            catch
            {
                message = string.Empty;
                return false;
            }
        }
    }
}

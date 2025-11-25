using System;
using System.IO;

namespace ClientApp
{
    public static class Logger
    {
        private const string LogFile = "client.log";

        public static void Write(string message)
        {
            File.AppendAllText(LogFile, $"[{DateTime.Now}] {message}\n");
        }
    }
}

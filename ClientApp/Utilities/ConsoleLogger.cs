using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientApp.Utilities
{
    public static class ConsoleLogger
    {
        private static readonly object _lock = new();
        public static void Success(string msg) => Write(msg, ConsoleColor.Green);
        public static void Info(string msg) => Write(msg, ConsoleColor.Cyan);
        public static void Receive(string msg) => Write(msg, ConsoleColor.Yellow);
        public static void Private(string msg) => Write($"{msg}", ConsoleColor.Magenta);
        public static void Error(string msg) => Write($"[ERROR] {msg}", ConsoleColor.Red);

        private static void Write(string msg, ConsoleColor color)
        {
            lock (_lock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }
    }
}

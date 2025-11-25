using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp.Utilities
{
    public static class ConsoleLogger
    {
        public static void Success(string msg) => Write(msg, ConsoleColor.Green);
        public static void Error(string msg) => Write($"[ERROR] {msg}", ConsoleColor.Red);
        public static void Join(string msg) => Write($"[JOIN] {msg}", ConsoleColor.Cyan);
        public static void Broadcast(string msg) => Write($"[BROADCAST] {msg}", ConsoleColor.Yellow);
        public static void Private(string msg) => Write($"[PRIVATE] {msg}", ConsoleColor.Magenta);
        public static void Info(string msg) => Write($"[INFO] {msg}", ConsoleColor.White);

        private static void Write(string msg, ConsoleColor color)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = color;
                Console.WriteLine(msg);
                Console.ResetColor();
            }
        }
    }
}

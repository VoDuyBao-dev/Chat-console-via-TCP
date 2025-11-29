using System;

namespace ClientApp.Utilities
{
    public static class TextAlign
    {
        public static string AlignRight(string text)
        {
            int width;

            try
            {
                width = Console.WindowWidth;
            }
            catch
            {
                width = 80; // fallback
            }

            // nếu width quá nhỏ do terminal không hỗ trợ
            if (width < 20)
                width = 80;

            int padding = width - text.Length - 2;
            if (padding < 0) padding = 0;

            return new string(' ', padding) + text;
        }

        public static string AlignLeft(string text)
        {
            return text;
        }
    }
}

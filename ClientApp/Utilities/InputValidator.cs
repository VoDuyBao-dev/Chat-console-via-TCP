using System;

namespace ClientApp.Utilities
{
    public static class InputValidator
    {
        public static bool IsInvalid(string? input)
            => string.IsNullOrWhiteSpace(input);

        public static bool ContainsIllegalChars(string input)
        {
            char[] illegal = { '|', '/', '\\', ':', ';', '\'', '"', '<', '>', '{', '}', '[', ']' };
            return input.Any(c => illegal.Contains(c));
        }

        // ===== PASSWORD MASKING =====
        public static string ReadPassword()
        {
            string pass = "";
            ConsoleKeyInfo key;

            while (true)
            {
                key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (pass.Length > 0)
                    {
                        pass = pass[..^1];
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
            }

            return pass;
        }
    }
}

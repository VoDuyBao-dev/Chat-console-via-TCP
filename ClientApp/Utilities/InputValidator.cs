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
    }
}

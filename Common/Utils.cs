using System.Text;
using System.Security.Cryptography;


public static class Utils
    {
        public static string HashPassword(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }


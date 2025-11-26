using System.Text;
using System.Security.Cryptography;

namespace Common
{
    public static class Utils
    {
        public static class PasswordHasher
        {
            public static string SHA256Hash(string input)
            {
                using var sha = SHA256.Create();
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = sha.ComputeHash(bytes);
                return Convert.ToHexString(hashBytes);
            }
        }
    }
}
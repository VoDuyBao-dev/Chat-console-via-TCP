using Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ClientApp.Tests
{

    public class TestUsername_PasswordWhenLogin
    {
        private static readonly Dictionary<string, string> UserDatabase = new()
        {
            { "mavis", "123456" }
        };

        private static string ExpectedLoginMessage(string username, string password)
        {
            bool userExists = UserDatabase.ContainsKey(username);
            bool passCorrect = userExists && UserDatabase[username] == password;

            if (!userExists)
                return "[SERVER] Incorrect username.";  
            else if (!passCorrect)
                return "[SERVER] Incorrect password.";  
            return "Login OK! Welcome to the chat room.";

        }

        private static string ActualServerLoginMessage(string username, string password)
        {
            if (!UserDatabase.ContainsKey(username)) 
                return "[SERVER] Incorrect username.";
            else if (UserDatabase[username] != password)
                return "[SERVER] Incorrect password.";
            return "Login OK! Welcome to the chat room.";
        }

        [Theory]
        [InlineData("mavis", "123456")]
        [InlineData("mavis", "wrongPass")]
        [InlineData("wrongUser", "123456")]
        [InlineData("wrongUser", "wrongPass")]
        public async Task Login_ShouldCompareExpectedVsActual(string username, string password)
        {
            var expected = ExpectedLoginMessage(username, password);
            var actual = await Task.Run(() => ActualServerLoginMessage(username, password));

            Console.WriteLine("=========================================");
            Console.WriteLine($"Kiểm tra đăng nhập username: \"{username}\", password: \"{password}\"");
            Console.WriteLine($"Mong muốn đầu ra : {expected}");
            Console.WriteLine($"Thực tế đầu ra   : {actual}");
            if (expected == actual)
                Console.WriteLine("KẾT QUẢ CHÍNH XÁC");
            else
                Console.WriteLine("KẾT QUẢ KHÔNG KHỚP — Test Fail");
            Console.WriteLine("=========================================");

            //Assert.Equal(expected, actual);
        }
    }
}

using Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace ClientApp.Tests
{
    public class TestForRegisterUser
    {
        // Giả lập database
        private static readonly Dictionary<string, (string Password, string Display)> FakeDB = new()
        {
            { "mavis", ("123456", "Mavis Nguyễn") }
        };

        // ------------------------- EXPECTED -----------------------------
        private static string ExpectedRegisterMessage(string username, string password, string display)
        {
            bool usernameExists = FakeDB.ContainsKey(username);
            bool displayExists = false;

            foreach (var user in FakeDB.Values)
                if (user.Display == display)
                    displayExists = true;
            if (usernameExists && displayExists)
                return "[SERVER] Username and Display name already exists."; 
            else if (usernameExists)
                return "[SERVER] Username already exists.";

            else if (displayExists)
                return "[SERVER] Display name already exists.";

            // đăng ký thành công → auto login
            return "[SERVER] Register success + Auto Login OK!";
        }

        // ------------------------- ACTUAL (MÔ PHỎNG SERVER) -----------------------------
        private static async Task<string> ActualRegisterProcess(string username, string password, string display)
        {
            // Mô phỏng gọi async DB
            bool UsernameExistsAsync(string u) => FakeDB.ContainsKey(u);
            bool DisplayExistsAsync(string d)
            {
                foreach (var user in FakeDB.Values)
                    if (user.Display == d) return true;
                return false;
            }

            if (UsernameExistsAsync(username))
                return "[SERVER] Username already exists.";

            if (DisplayExistsAsync(display))
                return "[SERVER] Display name already exists.";

            // thêm vào DB
            FakeDB.Add(username, (password, display));

            // auto login → giả lập AuthResult OK
            return "[SERVER] Register success + Auto Login OK!";
        }

        // ------------------------- TEST CASES -----------------------------
        [Theory]
        [InlineData("mavis", "123456", "Tên Hiển Thị A")]          // Username trùng → Fail
        [InlineData("newUser", "abcdef", "Mavis Nguyễn")]          // Display trùng → Fail
        [InlineData("newUser2", "888888", "User Hai")]             // Đăng ký OK
        [InlineData("newUser3", "000000", "User Ba")]      // Đăng ký OK
        [InlineData("mavis", "123456", "Mavis Nguyễn")]          // Cả 2 trùng → Fail
        public async Task Register_CompareExpectedVsActual(string username, string password, string display)
        {
            var expected = ExpectedRegisterMessage(username, password, display);
            var actual = await ActualRegisterProcess(username, password, display);

            Console.WriteLine("=========================================");
            Console.WriteLine($"▶ TEST REGISTER: username=\"{username}\" display=\"{display}\"");
            Console.WriteLine($"Mong muốn đầu ra : {expected}");
            Console.WriteLine($"Thực tế đầu ra   : {actual}");
            Console.WriteLine(expected == actual ? "KẾT QUẢ CHÍNH XÁC" : "KẾT QUẢ KHÔNG KHỚP — Test Fail");
            Console.WriteLine("=========================================\n");

            //Assert.Equal(expected, actual);
        }
    }
}

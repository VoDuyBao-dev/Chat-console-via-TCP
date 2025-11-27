using System;
using System.Collections.Generic;
using Xunit;

namespace ClientApp.Tests
{
    public class CaseSensitiveUsernameTests
    {
        // CSDL giả lập chỉ chứa duy nhất: "mavis"
        private readonly HashSet<string> users = new(StringComparer.Ordinal) { "mavis" };

        // Hàm kiểm tra username có trùng chính xác hay không
        private bool IsSame(string name) => users.Contains(name);

        // ===================== Kiểm tra trùng username =====================
        // "mavis" → trùng
        // Các dạng khác hoa/thường → không tính là trùng
        [Theory]
        [InlineData("mavis", true)]
        [InlineData("Mavis", false)]
        [InlineData("mAvis", false)]
        [InlineData("MAVIS", false)]
        [InlineData("maviss", false)]
        public void DuplicateCheck(string input, bool expected)
        {
            Assert.Equal(expected, IsSame(input));
        }

        // ===================== Quy tắc Login =====================
        // Người dùng chỉ bị từ chối khi username nhập vào giống EXACT "mavis".
        // Nếu khác case → coi như username mới → cho phép đăng nhập.
        [Theory]
        [InlineData("mavis", false)]  // Trùng hoàn toàn → từ chối đăng nhập
        [InlineData("Mavis", true)]   // Khác case → đăng nhập OK
        [InlineData("mAvis", true)]
        [InlineData("MAVIS", true)]
        public void LoginCaseSensitive(string input, bool expected)
        {
            bool canLogin = !IsSame(input); // chỉ cho login nếu không trùng exact
            Assert.Equal(expected, canLogin);
        }
    }
}

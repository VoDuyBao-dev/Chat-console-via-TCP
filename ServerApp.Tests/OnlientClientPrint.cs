using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class User
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}

public class ServerDynamic
{
    private readonly List<User> _users;

    public ServerDynamic(List<User> users) => _users = users;

    // Giả lập server có thể trả Username hoặc DisplayName
    public string[] GetOnlineUsersForClient(bool sendDisplayName = true)
    {
        return _users
            .Where(u => u.IsOnline)
            .Select(u => sendDisplayName ? u.DisplayName : u.Username)
            .ToArray();
    }
}

public class OnlineUsersDynamicTests
{
    [Fact]
    public void GetOnlineUsers_AutoCheckEachUser()
    {
        var users = new List<User>
        {
            new User { Username = "mavis", DisplayName = "Mavis", IsOnline = true },
            new User { Username = "lucy", DisplayName = "Lucy", IsOnline = true },
            new User { Username = "hongyen", DisplayName = "Yen", IsOnline = false }
        };

        var server = new ServerDynamic(users);

        // Lấy kết quả từ server
        var actualList = server.GetOnlineUsersForClient();

        Console.WriteLine("=== Danh sách user online check tự động ===");

        foreach (var user in users)
        {
            if (user.IsOnline)
            {
                // Kiểm tra server trả có chứa DisplayName hay Username
                if (actualList.Contains(user.DisplayName))
                    Console.WriteLine($"{user.DisplayName} hiển thị đúng");
                else if (actualList.Contains(user.Username))
                    Console.WriteLine($"{user.Username} được hiển thị thay vì DisplayName");
                else
                    Console.WriteLine($"{user.DisplayName} online không xuất hiện");
            }
            
        }

        // Assert: kiểm tra tất cả user online phải có ít nhất 1 tên trong actualList
        var expectedOnlineCount = users.Count(u => u.IsOnline);
        Assert.Equal(expectedOnlineCount, actualList.Length);
    }
}

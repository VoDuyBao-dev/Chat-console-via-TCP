using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

// Model User rút gọn
public class User
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}

// Server hiện tại (gửi Username thay vì DisplayName)
public class ServerCurrent
{
    private readonly List<User> _users;

    public ServerCurrent(List<User> users) => _users = users;

    public string[] GetOnlineUsersForClient()
        => _users
            .Where(u => u.IsOnline)
            .Select(u => u.Username) //  hiện trạng server
            .ToArray();
}

public class OnlineUsersCurrentTests
{
    [Fact]
    public void GetOnlineUsers_ShouldReturnDisplayName_ButActuallyUsername()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Username = "mavis", DisplayName = "Mavis", IsOnline = true },
            new User { Username = "lucy", DisplayName = "Lucy", IsOnline = true },
            new User { Username = "Hongyen", DisplayName = "Yen", IsOnline = false }
        };

        var server = new ServerCurrent(users);

        // Act
        var actualList = server.GetOnlineUsersForClient();

        // Mong muốn: DisplayName
        var expectedList = users.Where(u => u.IsOnline)
                                .Select(u => u.DisplayName)
                                .ToArray();

        // Console output chi tiết
        Console.WriteLine("=== Danh sách user online check từng user ===");
        foreach (var user in users.Where(u => u.IsOnline))
        {
            if (actualList.Contains(user.DisplayName))
                Console.WriteLine($"{user.DisplayName} hiển thị đúng");
            else if (actualList.Contains(user.Username))
                Console.WriteLine($"❌ {user.Username} bị hiển thị thay vì DisplayName");
            else
                Console.WriteLine($"{user.DisplayName} không hiển thị");
        }

        foreach (var user in users.Where(u => !u.IsOnline))
        {
            if (!actualList.Contains(user.DisplayName) && !actualList.Contains(user.Username))
                Console.WriteLine($" " +
                    $"{user.DisplayName} offline không xuất hiện");
            else
                Console.WriteLine($"{user.DisplayName} offline vẫn xuất hiện");
        }

        // Assert: mong muốn = thực tế (test sẽ fail nếu server gửi Username)
        Assert.Equal(expectedList, actualList);
    }
}

using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class TestUsername_PasswordWhenLogin
{
    private const string SERVER_IP = "127.0.0.1";
    private const int SERVER_PORT = 5000;

    [Theory]
    [InlineData("wrongUser", "correctPass", "[SERVER] Sai username")]
    [InlineData("correctUser", "wrongPass", "[SERVER] Sai password")]
    [InlineData("wrongUser", "wrongPass", "[SERVER] Sai cả 2")]
    [InlineData("correctUser", "correctPass", "[SERVER] Login thành công")]
    public async Task Login_ShouldReturnExpectedMessage(string username, string password, string expected)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(SERVER_IP, SERVER_PORT);
        using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        // Gửi dữ liệu login
        await writer.WriteLineAsync(username);
        await writer.WriteLineAsync(password);

        // Đọc phản hồi từ server
        var response = await reader.ReadLineAsync();

        // So sánh với reference
        Assert.Equal(expected, response);
    }
}

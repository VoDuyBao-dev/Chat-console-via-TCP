using MySql.Data.MySqlClient;
using Common;

namespace ServerApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        private MySqlConnection GetConnection()
        {
            var conn = new MySqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        // 1) Kiểm tra username đã tồn tại chưa
        public bool IsUsernameTaken(string username)
        {
            using var conn = GetConnection();

            string query = "SELECT COUNT(*) FROM users WHERE Username = @u";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);

            long count = (long)cmd.ExecuteScalar();
            return count > 0;
        }

        // 2) Tạo user mới
        public bool CreateUser(string username, string passwordHash, string displayName)
        {
            using var conn = GetConnection();

            string query = @"
                INSERT INTO users (Username, PasswordHash, DisplayName, IsOnline)
                VALUES (@u, @p, @n, 0)";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);
            cmd.Parameters.AddWithValue("@n", displayName);

            return cmd.ExecuteNonQuery() > 0;
        }

        // 3) Lấy UserId từ Username
        public int? GetUserId(string username)
        {
            using var conn = GetConnection();

            string query = "SELECT UserId FROM users WHERE Username = @u LIMIT 1";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);

            object? result = cmd.ExecuteScalar();
            if (result == null) return null;

            return Convert.ToInt32(result);
        }

        // 4) Xác thực user khi đăng nhập
        public bool ValidateUser(string username, string rawPassword)
        {
            using var conn = GetConnection();

            string query = "SELECT PasswordHash FROM users WHERE Username = @u LIMIT 1";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);

            object? result = cmd.ExecuteScalar();
            if (result == null) return false;

            string storedHash = result.ToString()!;
            string enteredHash = Utils.HashPassword(rawPassword);

            return storedHash.Equals(enteredHash, StringComparison.OrdinalIgnoreCase);
        }

        // 5) Set IsOnline = 1 khi đăng nhập thành công
        public void SetOnline(string username)
        {
            using var conn = GetConnection();

            string query = "UPDATE users SET IsOnline = 1 WHERE Username = @u";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);

            cmd.ExecuteNonQuery();
        }

        // 6) Set IsOnline = 0 khi thoát/mất kết nối
        public void SetOffline(string username)
        {
            using var conn = GetConnection();

            string query = "UPDATE users SET IsOnline = 0 WHERE Username = @u";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);

            cmd.ExecuteNonQuery();
        }

        // 7) Lưu tin nhắn
        public void SaveMessage(int? senderId, int? receiverId, string content, int messageType)
        {
            using var conn = GetConnection();

            string query = @"
                INSERT INTO messages (SenderId, ReceiverId, Content, MessageType)
                VALUES (@s, @r, @c, @t)
            ";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@s", senderId.HasValue ? senderId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@r", receiverId.HasValue ? receiverId.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@c", content);
            cmd.Parameters.AddWithValue("@t", messageType);

            cmd.ExecuteNonQuery();
        }
    }
}

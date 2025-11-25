using MySql.Data.MySqlClient;
using System.Threading.Tasks;

namespace ServerApp.Services
{
    public class DatabaseService
    {
        private readonly string _connStr;

        public DatabaseService(string connStr)
        {
            _connStr = connStr;
        }

        private MySqlConnection GetConn() => new MySqlConnection(_connStr);

        public async Task<bool> UsernameExistsAsync(string username)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = "SELECT COUNT(*) FROM users WHERE Username=@u";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);

            long count = (long)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

        public async Task<bool> RegisterAsync(string username, string passwordHash, string displayName)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"INSERT INTO users (Username, PasswordHash, DisplayName)
                           VALUES (@u, @p, @d)";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);
            cmd.Parameters.AddWithValue("@d", displayName);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<(int UserId, string DisplayName)?> LoginAsync(string username, string passwordHash)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"SELECT UserId, DisplayName 
                           FROM users
                           WHERE Username=@u AND PasswordHash=@p";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(UserId), reader.GetString("DisplayName"));
            }
            return null;
        }

        public async Task SetOnlineAsync(int userId)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"UPDATE users 
                           SET IsOnline = 1, LastLogin = NOW() 
                           WHERE UserId=@id";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SetOfflineAsync(int userId)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"UPDATE users 
                           SET IsOnline = 0, LastLogout = NOW() 
                           WHERE UserId=@id";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", userId);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task SaveMessageAsync(int senderId, int receiverId, string content)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"INSERT INTO messages (SenderId, ReceiverId, Content)
                           VALUES (@s, @r, @c)";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@s", senderId);
            cmd.Parameters.AddWithValue("@r", receiverId);
            cmd.Parameters.AddWithValue("@c", content);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}

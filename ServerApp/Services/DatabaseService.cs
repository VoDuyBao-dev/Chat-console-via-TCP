using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using ServerApp.Models;


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

        public async Task<bool> DisplayExistsAsync(string displayName)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"SELECT COUNT(*) 
                   FROM users 
                   WHERE  DisplayName = @d";

            var cmd = new MySqlCommand(sql, conn);
            // cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@d", displayName);

            long count = (long)await cmd.ExecuteScalarAsync();
            return count > 0;
        }

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


        public async Task<bool> PasswordMatchAsync(string username, string passwordHash)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"SELECT 1 FROM users 
                   WHERE Username=@u AND PasswordHash=@p 
                   LIMIT 1";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
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

        public async Task<(int UserId, string DisplayName, string PasswordHash)?> GetUserByUsernameAsync(string username)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"SELECT UserId, DisplayName, PasswordHash
                   FROM users
                   WHERE Username=@u
                   LIMIT 1";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return
                (
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                    reader.GetString(2)
                );
            }

            return null;
        }
        public async Task<List<ChatMessage>> GetChatHistoryAsync(int userA, int userB)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[DEBUG] GetChatHistoryAsync(userA={userA}, userB={userB}) | Types -> {userA.GetType()}, {userB.GetType()}");

            var list = new List<ChatMessage>();

            using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            string sql = @"
                SELECT 
                    m.Content,
                    m.SentTime,
                    u.DisplayName AS FromDisplay
                FROM messages m
                JOIN users u ON u.UserId = m.SenderId
                WHERE 
                    (m.SenderId = @A AND m.ReceiverId = @B)
                    OR
                    (m.SenderId = @B AND m.ReceiverId = @A)
                ORDER BY m.SentTime ASC;
            ";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@A", userA);
            cmd.Parameters.AddWithValue("@B", userB);

            using var reader = await cmd.ExecuteReaderAsync();

            int colContent = reader.GetOrdinal("Content");
            int colSent = reader.GetOrdinal("SentTime");
            int colFrom = reader.GetOrdinal("FromDisplay");

            while (await reader.ReadAsync())
            {
                list.Add(new ChatMessage
                {
                    FromDisplay = reader.IsDBNull(colFrom) ? "" : reader.GetString(colFrom),
                    Message = reader.IsDBNull(colContent) ? "" : reader.GetString(colContent),
                    Timestamp = reader.GetDateTime(colSent)
                });
            }
            return list;
        }
        public async Task<(int UserId, string DisplayName)?> GetUserByDisplayNameAsync(string displayName)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"SELECT UserId, DisplayName
                   FROM users
                   WHERE DisplayName = @d
                   LIMIT 1";

            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@d", displayName);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return (reader.GetInt32(0), reader.GetString(1));
            }

            return null;
        }

    }
}

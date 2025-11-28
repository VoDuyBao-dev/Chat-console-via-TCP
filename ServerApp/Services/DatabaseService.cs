using MySql.Data.MySqlClient;
using ServerApp.Models;
using System.Data;
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

        // chat group
        // Lấy tất cả nhóm (dùng khi khởi động server)
        public async Task<List<ChatGroup>> GetAllGroupsAsync()
        {
            var list = new List<ChatGroup>();
            using var conn = new MySqlConnection(_connStr);
            await conn.OpenAsync();

            using var cmd = new MySqlCommand("SELECT GroupId, GroupName, CreatorId FROM chat_groups", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ChatGroup
                {
                    GroupId = reader.GetInt32("GroupId"),
                    GroupName = reader.GetString("GroupName"),
                    CreatorId = reader.GetInt32("CreatorId")
                });
            }
            return list;
        }
        /// Tạo nhóm chat mới, trả về GroupId
        public async Task<int> CreateGroupAsync(int creatorId, string groupName)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            // Kiểm tra tên nhóm đã tồn tại chưa
            string checkSql = "SELECT COUNT(*) FROM chat_groups WHERE GroupName = @name";
            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@name", groupName);
                long exists = (long)await checkCmd.ExecuteScalarAsync();
                if (exists > 0)
                    return -1; // tên nhóm đã tồn tại
            }

            string sql = @"INSERT INTO chat_groups (GroupName, CreatorId) 
                        VALUES (@name, @creator);
                        SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", groupName);
            cmd.Parameters.AddWithValue("@creator", creatorId);

            var result = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// Thêm người dùng vào nhóm
        public async Task<bool> AddUserToGroupAsync(int groupId, int userId, string role = "member")
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            // Tránh thêm trùng
            string checkSql = @"SELECT COUNT(*) FROM group_members 
                                WHERE GroupId = @gid AND UserId = @uid";
            using (var checkCmd = new MySqlCommand(checkSql, conn))
            {
                checkCmd.Parameters.AddWithValue("@gid", groupId);
                checkCmd.Parameters.AddWithValue("@uid", userId);
                long count = (long)await checkCmd.ExecuteScalarAsync();
                if (count > 0) return false; // đã trong nhóm rồi
            }

            string sql = @"INSERT INTO group_members (GroupId, UserId, Role) 
                   VALUES (@gid, @uid, @role)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@gid", groupId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@role", role);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        /// Kiểm tra user có phải admin/creator của nhóm không. mặc định creator là admin luôn 
        public async Task<bool> IsGroupAdminAsync(int groupId, int userId)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 1 
                FROM group_members 
                WHERE GroupId = @gid 
                AND UserId = @uid 
                AND Role = 'admin' 
                LIMIT 1";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@gid", groupId);
            cmd.Parameters.AddWithValue("@uid", userId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }
// Kiểm tra user có trong nhóm không
        public async Task<bool> IsUserInGroupAsync(int groupId, int userId)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            const string sql = @"
                SELECT 1 
                FROM group_members 
                WHERE GroupId = @gid AND UserId = @uid 
                LIMIT 1";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@gid", groupId);
            cmd.Parameters.AddWithValue("@uid", userId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }

        /// Lưu tin nhắn nhóm vào bảng group_messages
        public async Task SaveGroupMessageAsync(int groupId, int senderId, string content)
        {
            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"INSERT INTO group_messages (GroupId, SenderId, Content)
                        VALUES (@gid, @sid, @c)";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@gid", groupId);
            cmd.Parameters.AddWithValue("@sid", senderId);
            cmd.Parameters.AddWithValue("@c", content);

            await cmd.ExecuteNonQueryAsync();
        }

         /// Lấy danh sách nhóm mà user đang tham gia
        public async Task<List<ChatGroup>> GetUserGroupsAsync(int userId)
        {
            var list = new List<ChatGroup>();

            using var conn = GetConn();
            await conn.OpenAsync();

            string sql = @"
                SELECT g.GroupId, g.GroupName, g.CreatorId, g.CreatedAt
                FROM chat_groups g
                INNER JOIN group_members gm ON g.GroupId = gm.GroupId
                WHERE gm.UserId = @uid
                ORDER BY g.CreatedAt DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ChatGroup
                {
                    GroupId = reader.GetInt32("GroupId"),
                    GroupName = reader.GetString("GroupName"),
                    CreatorId = reader.GetInt32("CreatorId"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }

            return list;
        }

        





       
        


    }
}

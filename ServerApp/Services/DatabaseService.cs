using MySql.Data.MySqlClient;

namespace ServerApp.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 1) Kiểm tra username đã tồn tại chưa
        public bool IsUsernameTaken(string username)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();
            string query = "SELECT COUNT(*) FROM users WHERE Username = @u";

            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);

            long count = (long)cmd.ExecuteScalar();
            return count > 0;
        }

        // 2) Tạo user mới
        public bool CreateUser(string username, string passwordHash, string displayName)
        {
            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string query = @"
                INSERT INTO users (Username, PasswordHash, DisplayName, IsOnline)
                VALUES (@u, @p, @n, 0)";
            
            using var cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.Parameters.AddWithValue("@p", passwordHash);
            cmd.Parameters.AddWithValue("@n", displayName);

            return cmd.ExecuteNonQuery() > 0;
        }
    }
}

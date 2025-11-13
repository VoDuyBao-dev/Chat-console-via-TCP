using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using MySql.Data.MySqlClient;
using ServerApp.Models;

namespace ServerApp
{
    public class UserManager
    {
        // ket noi MySQL
        private readonly string _connectionString =
            "Server=localhost;Database=chatconsoletcp;User ID=root;Password=;SslMode=none;AllowPublicKeyRetrieval=True;";

        // dang nhap
        public bool TryLogin(string username, string password, TcpClient client, out string message)
        {
            message = "";

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = "SELECT UserId, Username, PasswordHash, DisplayName FROM Users WHERE Username = @u";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                message = "Tài khoản không tồn tại.";
                return false;
            }

            var dbPassword = reader["PasswordHash"].ToString();
            if (dbPassword != password) // TODO: co the thay the bang hash sau nay
            {
                message = "Sai mật khẩu.";
                return false;
            }

            if (_onlineUsers.ContainsKey(username))
            {
                message = "Người dùng này đã đăng nhập ở nơi khác.";
                return false;
            }

            var user = new User
            {
                UserId = Convert.ToInt32(reader["UserId"]),
                Username = username,
                DisplayName = reader["DisplayName"].ToString(),
                IsOnline = true,
                Client = client,
                LastLogin = DateTime.Now
            };

            reader.Close();

            // Cap nhat DB: online = 1
            string updateSql = "UPDATE Users SET IsOnline = 1, LastLogin = NOW() WHERE Username = @u";
            using var updateCmd = new MySqlCommand(updateSql, conn);
            updateCmd.Parameters.AddWithValue("@u", username);
            updateCmd.ExecuteNonQuery();

            _onlineUsers[username] = user;

            message = "Đăng nhập thành công.";
            return true;
        }

        // đang xuat
        public bool Logout(string username)
        {
            if (!_onlineUsers.TryRemove(username, out _))
                return false;

            using var conn = new MySqlConnection(_connectionString);
            conn.Open();

            string sql = "UPDATE Users SET IsOnline = 0, LastLogout = NOW() WHERE Username = @u";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@u", username);
            cmd.ExecuteNonQuery();

            return true;
        }


        // kiem tra user online
        public bool IsOnline(string username)
        {
            return _onlineUsers.ContainsKey(username);
        }
    }
}

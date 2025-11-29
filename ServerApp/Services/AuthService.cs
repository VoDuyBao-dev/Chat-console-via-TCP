using System.Collections.Concurrent;
using System.Threading.Tasks;
using Common;
using ServerApp.Models;
using ServerApp.Utilities;
namespace ServerApp.Services
{
    public class AuthService
    {
        private readonly DatabaseService _db;
        private readonly ConcurrentDictionary<string, User> _clients;

        public AuthService(DatabaseService db, ConcurrentDictionary<string, User> clients)
        {
            _db = db;
            _clients = clients;
        }

        // REGISTER
        public async Task<AuthResult> HandleRegisterAsync(User user, string[] args)
        {
            if (args.Length < 3)
                return AuthResult.Fail("[SERVER] Invalid REGISTER format.");

            string username = args[0];
            string passHash = args[1];
            string display = args[2];

            if (await _db.UsernameExistsAsync(username) && await _db.DisplayExistsAsync(display))
                return AuthResult.Fail("[SERVER] Username and display name already exists.");
            else if (await _db.UsernameExistsAsync(username))
                return AuthResult.Fail("[SERVER] Username already exists.");
            else if (await _db.DisplayExistsAsync(display))
                return AuthResult.Fail("[SERVER] Display name already exists.");


            await _db.RegisterAsync(username, passHash, display);

            // Auto login
            var newUser = await _db.GetUserByUsernameAsync(username);
            if (newUser == null)
                return AuthResult.Fail("[SERVER] Registration error. Cannot auto-login.");

            user.UserId = newUser.Value.UserId;
            user.Username = username;
            user.DisplayName = newUser.Value.DisplayName;

            await _db.SetOnlineAsync(user.UserId);
            _clients.TryAdd(user.Username, user);

            return AuthResult.Ok($"{Protocol.REGISTER_SUCCESS}{Protocol.Split}{username}{Protocol.Split}{display}");
        }

        // LOGIN
        public async Task<AuthResult> HandleLoginAsync(User user, string[] args)
        {
            if (args.Length < 2)
                return AuthResult.Fail("[SERVER] Invalid LOGIN format.");

            string username = args[0];
            string passHash = args[1];

            if (_clients.ContainsKey(username))
                return AuthResult.Fail("[SERVER] This account is already logged in.");


            // 1) Lấy user theo username
            var dbUser = await _db.GetUserByUsernameAsync(username);
            if (dbUser == null)
                return AuthResult.Fail("[SERVER] Incorrect username.");


            // 2) Kiểm tra password
            if (!string.Equals(dbUser.Value.PasswordHash, passHash, StringComparison.Ordinal))
                return AuthResult.Fail("[SERVER] Incorrect password.");

            // 3) Đăng nhập OK
            user.UserId = dbUser.Value.UserId;
            user.Username = username;
            user.DisplayName = dbUser.Value.DisplayName;
       
            await _db.SetOnlineAsync(user.UserId);
            _clients.TryAdd(user.Username, user);

            // return AuthResult.Ok(Protocol.LOGIN_SUCCESS);
            return AuthResult.Ok($"{Protocol.LOGIN_SUCCESS}{Protocol.Split}{username}{Protocol.Split}{dbUser.Value.DisplayName}");
        }

    }
}

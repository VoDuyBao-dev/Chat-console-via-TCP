using System.Text.Json;

namespace Common
{
    public class ConfigService
    {
        public static string GetConnectionString()
        {
            // var json = File.ReadAllText("appsettings.json");
            string path = Path.Combine(
                AppContext.BaseDirectory,        // thư mục bin/Debug/net8.0
                "appsettings.json"
            );

            var json = File.ReadAllText(path);

            var config = JsonSerializer.Deserialize<Root>(json)
                         ?? throw new InvalidOperationException("Không đọc được cấu hình Database từ appsettings.json");

            var db = config.Database
                     ?? throw new InvalidOperationException("Thiếu mục 'Database' trong appsettings.json");

            return $"server={db.Host};port={db.Port};user={db.User};password={db.Password};database={db.DatabaseName}";
        }
    }

    public class Root
    {
        public DatabaseConfig? Database { get; set; } = new();
    }

    public class DatabaseConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DatabaseName { get; set; }
    }
}

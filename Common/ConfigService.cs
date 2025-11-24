using System.Text.Json;

namespace Common
{
    public class ConfigService
    {
        public static string GetConnectionString()
        {
            var json = File.ReadAllText("appsettings.json");
            var config = JsonSerializer.Deserialize<Root>(json);

            var db = config.Database;

            return $"server={db.Host};port={db.Port};user={db.User};password={db.Password};database={db.DatabaseName}";
        }
    }

    public class Root
    {
        public DatabaseConfig Database { get; set; }
    }

    public class DatabaseConfig
    {
        public string Host { get; set; }
        public int    Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DatabaseName { get; set; }
    }
}

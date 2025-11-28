namespace ServerApp.Models
{
    public class AuthResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = "";
        public string SuccessToken { get; set; } = "";

        public static AuthResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };

        public static AuthResult Ok(string token) =>
            new() { Success = true, SuccessToken = token };
    }
}

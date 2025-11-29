// // UserSession.cs
// namespace ClientApp
// {
//     public class UserSession
//     {
//         public string? Username { get; private set; }
//         public bool IsLoggedIn { get; private set; } = false;
//         public bool IsWaitingAuth { get; set; } = false;

//         public void Login(string username)
//         {
//             Username = username;
//             IsLoggedIn = true;
//         }

//         public void ResetAuthWait()
//         {
//             IsWaitingAuth = false;
//             IsLoggedIn = false;
//         }

//         public void RestoreLogin()
//         {
//             IsLoggedIn = true;
//         }
//     }
// }

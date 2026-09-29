using MshMsh.Domain;

namespace MshMsh.Services
{
    public static class CurrentSession
    {
        // المستخدم اللي مسجل دخول حالياً
        public static User? CurrentUser { get; private set; }

        public static void Login(User user)
        {
            CurrentUser = user;
        }

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static bool IsAdmin => CurrentUser?.Role == UserRole.Admin;
    }
}
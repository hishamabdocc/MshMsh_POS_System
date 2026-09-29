using Microsoft.EntityFrameworkCore;
using MshMsh.Data;
using MshMsh.Domain;

namespace MshMsh.Services
{
    public class AuthService
    {
        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            using var context = new AppDbContext();

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username.Trim().ToLower() && u.Password == password);

            if (user != null)
            {
                // حفظ المستخدم في الجلسة الحالية
                CurrentSession.Login(user);
            }

            return user;
        }
    }
}
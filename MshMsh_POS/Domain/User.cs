namespace MshMsh.Domain
{
    public enum UserRole
    {
        Admin,      // صاحب الشغل / المدير
        Cashier     // كاشير
    }

    public class User
    {
        public int Id { get; private set; }
        public string Username { get; private set; } = null!;
        public string Password { get; private set; } = null!; // في المشاريع الكبيرة يُفضل Hash
        public string FullName { get; private set; } = null!;
        public UserRole Role { get; private set; }

        private User() { }

        public User(string username, string password, string fullName, UserRole role)
        {
            Validate(username, password, fullName);

            Username = username.Trim().ToLower();
            Password = password;
            FullName = fullName.Trim();
            Role = role;
        }

        public void UpdatePassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4)
                throw new ArgumentException("كلمة المرور يجب ألا تقل عن 4 خانات.");

            Password = newPassword;
        }

        private void Validate(string username, string password, string fullName)
        {
            if (string.IsNullOrWhiteSpace(username) || username.Trim().Length < 3)
                throw new ArgumentException("اسم المستخدم يجب أن يكون 3 أحرف على الأقل.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
                throw new ArgumentException("كلمة المرور يجب ألا تقل عن 4 خانات.");

            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentException("الاسم بالكامل مطلوب.");
        }
    }
}
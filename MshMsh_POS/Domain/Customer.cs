using System.Text.RegularExpressions;

namespace MshMsh.Domain
{
    public class Customer
    {
        public int Id { get; private set; }
        public string Name { get; private set; } = null!;
        public string Phone { get; private set; } = null!;

        // Constructor فارغ مطلوب لـ EF Core
        private Customer() { }

        // Constructor الإنشاء
        public Customer(string name, string phone)
        {
            Validate(name, phone);

            Name = name.Trim();
            Phone = phone.Trim();
        }

        // دالة تعديل بيانات العميل
        public void Update(string name, string phone)
        {
            Validate(name, phone);

            Name = name.Trim();
            Phone = phone.Trim();
        }

        // قواعد التحقق (Domain Validation)
        private void Validate(string name, string phone)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم العميل لا يمكن أن يكون فارغاً.");

            if (name.Trim().Length < 2)
                throw new ArgumentException("اسم العميل يجب أن يحتوي على حرفين على الأقل.");

            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("رقم الهاتف لا يمكن أن يكون فارغاً.");

            // التحقق من أن رقم الهاتف أرقام فقط وطوله بين 10 و 15 رقم
            var phoneClean = phone.Trim();
            if (!Regex.IsMatch(phoneClean, @"^[0-9]{10,15}$"))
                throw new ArgumentException("رقم الهاتف غير صالح، يجب أن يحتوي على أرقام فقط ويتراوح بين 10 إلى 15 رقماً.");
        }
    }
}
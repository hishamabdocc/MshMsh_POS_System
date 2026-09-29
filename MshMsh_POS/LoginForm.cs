using MshMsh.Services;

namespace MshMsh
{
    public partial class LoginForm : Form
    {
        private readonly AuthService _authService = new();
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;

        public LoginForm()
        {
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "تسجيل الدخول - نظام مشمش POS";
            this.Size = new Size(380, 260);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.RightToLeft = RightToLeft.Yes;
            this.RightToLeftLayout = true;

            var lblTitle = new Label { Text = "تسجيل الدخول للنظام", Font = new Font("Segoe UI", 12, FontStyle.Bold), Top = 20, Left = 20, AutoSize = true };
            var lblUser = new Label { Text = "اسم المستخدم:", Top = 65, Left = 20, AutoSize = true };
            txtUsername = new TextBox { Top = 62, Left = 120, Width = 200 };

            var lblPass = new Label { Text = "كلمة المرور:", Top = 105, Left = 20, AutoSize = true };
            txtPassword = new TextBox { Top = 102, Left = 120, Width = 200, PasswordChar = '*' };

            btnLogin = new Button { Text = "دخول", Top = 150, Left = 120, Width = 200, Height = 35, BackColor = Color.DarkSlateBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnLogin.Click += async (s, e) => await HandleLogin();

            this.Controls.AddRange(new Control[] { lblTitle, lblUser, txtUsername, lblPass, txtPassword, btnLogin });
            this.AcceptButton = btnLogin;
        }

        private async Task HandleLogin()
        {
            btnLogin.Enabled = false;
            btnLogin.Text = "جاري التحقق...";

            var user = await _authService.AuthenticateAsync(txtUsername.Text, txtPassword.Text);

            if (user != null)
            {
                this.Hide();
                var mainForm = new Form1();
                mainForm.FormClosed += (s, args) => this.Close();
                mainForm.Show();
            }
            else
            {
                MessageBox.Show("اسم المستخدم أو كلمة المرور غير صحيحة!", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnLogin.Enabled = true;
                btnLogin.Text = "دخول";
            }
        }
    }
}
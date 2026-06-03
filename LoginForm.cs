using FontAwesome.Sharp;
using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Views;

public class LoginForm : Form
{
    private readonly AuthController _auth;
    private readonly Guna2TextBox _txtUsername = new();
    private readonly Guna2TextBox _txtPassword = new();
    private readonly Guna2CheckBox _chkRemember = new();
    private readonly Label _lblError = new();

    public LoginForm(AuthController auth)
    {
        _auth = auth;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = $"{AppConstants.AppName} - Login";
        Size = new Size(480, 580);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.None;
        UiHelper.StyleForm(this);

        var panel = new Guna2Panel
        {
            Size = new Size(420, 520),
            Location = new Point(30, 30),
            FillColor = ThemeColors.Card,
            BorderRadius = 16,
            ShadowDecoration = { Enabled = true, Depth = 15 }
        };

        var icon = new IconPictureBox
        {
            IconChar = IconChar.ShieldHalved,
            IconColor = ThemeColors.Primary,
            IconSize = 48,
            Location = new Point(186, 30),
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = AppConstants.AppName,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = ThemeColors.Text,
            AutoSize = false,
            Size = new Size(420, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 90),
            BackColor = Color.Transparent
        };

        var lblSub = new Label
        {
            Text = "Security Information & Event Management",
            ForeColor = ThemeColors.TextMuted,
            AutoSize = false,
            Size = new Size(420, 25),
            TextAlign = ContentAlignment.MiddleCenter,
            Location = new Point(0, 130),
            BackColor = Color.Transparent
        };

        _txtUsername.PlaceholderText = "Username";
        _txtUsername.Location = new Point(40, 190);
        _txtUsername.Size = new Size(340, 45);
        _txtUsername.BorderRadius = 8;
        _txtUsername.FillColor = Color.FromArgb(15, 23, 42);
        _txtUsername.ForeColor = ThemeColors.Text;
        _txtUsername.BorderColor = ThemeColors.Border;

        _txtPassword.PlaceholderText = "Password";
        _txtPassword.Location = new Point(40, 250);
        _txtPassword.Size = new Size(340, 45);
        _txtPassword.BorderRadius = 8;
        _txtPassword.UseSystemPasswordChar = true;
        _txtPassword.FillColor = Color.FromArgb(15, 23, 42);
        _txtPassword.ForeColor = ThemeColors.Text;
        _txtPassword.BorderColor = ThemeColors.Border;

        _chkRemember.Text = "Remember Me";
        _chkRemember.Location = new Point(40, 310);
        _chkRemember.ForeColor = ThemeColors.TextMuted;
        _chkRemember.CheckedState.FillColor = ThemeColors.Primary;

        _lblError.ForeColor = ThemeColors.Danger;
        _lblError.Location = new Point(40, 345);
        _lblError.Size = new Size(340, 25);
        _lblError.TextAlign = ContentAlignment.MiddleCenter;

        var btnLogin = UiHelper.CreateButton("Sign In", IconChar.SignInAlt, new Point(40, 380), new Size(340, 48));
        btnLogin.Click += async (_, _) => await LoginAsync();

        _txtPassword.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) await LoginAsync();
        };

        if (!string.IsNullOrEmpty(SessionManager.RememberedUsername))
        {
            _txtUsername.Text = SessionManager.RememberedUsername;
            _chkRemember.Checked = true;
        }

        panel.Controls.AddRange([icon, lblTitle, lblSub, _txtUsername, _txtPassword, _chkRemember, _lblError, btnLogin]);
        Controls.Add(panel);
    }

    private async Task LoginAsync()
    {
        _lblError.Text = "";
        var result = await _auth.LoginAsync(_txtUsername.Text, _txtPassword.Text, _chkRemember.Checked);
        if (result.Success)
        {
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            _lblError.Text = result.Message;
        }
    }
}

using FontAwesome.Sharp;
using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Services;
using CyberWatchSIEM.Utils;
using CyberWatchSIEM.Views.Modules;

namespace CyberWatchSIEM.Views;

public class MainShellForm : Form
{
    private readonly AuthController _auth;
    private readonly DashboardController _dashboard;
    private readonly LogController _logs;
    private readonly AlertController _alerts;
    private readonly IncidentController _incidents;
    private readonly UserController _users;
    private readonly SettingsController _settings;
    private readonly ReportController _reports;
    private readonly SearchController _search;
    private readonly SimulatorController _simulator;
    private readonly NotificationService _notifications;
    private readonly ThreatIntelController _threatIntel;
    private readonly AuditController _audit;
    private readonly LiveDataController _liveData;
    private readonly Label _lblLiveStatus = new();

    private readonly Panel _contentPanel = new();
    private readonly Label _lblPageTitle = new();
    private readonly Label _lblUser = new();
    private readonly Guna2TextBox _searchBox = new();
    private readonly List<Guna2Button> _navButtons = new();
    private System.Windows.Forms.Timer? _refreshTimer;
    private UserControl? _currentView;

    public MainShellForm(
        AuthController auth, DashboardController dashboard, LogController logs,
        AlertController alerts, IncidentController incidents, UserController users,
        SettingsController settings, ReportController reports, SearchController search,
        SimulatorController simulator, NotificationService notifications,
        ThreatIntelController threatIntel, AuditController audit, LiveDataController liveData)
    {
        _auth = auth;
        _dashboard = dashboard;
        _logs = logs;
        _alerts = alerts;
        _incidents = incidents;
        _users = users;
        _settings = settings;
        _reports = reports;
        _search = search;
        _simulator = simulator;
        _notifications = notifications;
        _threatIntel = threatIntel;
        _audit = audit;
        _liveData = liveData;

        InitializeComponent();
        UpdateLiveStatusBadge();
        _notifications.NotificationRaised += ShowNotification;
        _simulator.Start();
        NavigateTo("Dashboard", new DashboardView(_dashboard));
        StartAutoRefresh();
    }

    private void InitializeComponent()
    {
        Text = AppConstants.AppName;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1200, 700);
        UiHelper.StyleForm(this);

        var sidebar = new Guna2Panel
        {
            Dock = DockStyle.Left,
            Width = 240,
            FillColor = ThemeColors.Sidebar,
            BorderColor = ThemeColors.Border,
            BorderThickness = 1
        };

        var logo = new Label
        {
            Text = "  CyberWatch",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = ThemeColors.Primary,
            Dock = DockStyle.Top,
            Height = 60,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(15, 0, 0, 0)
        };

        var navPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 10, 0) };

        var navItems = new (string Name, IconChar Icon)[]
        {
            ("Dashboard", IconChar.ChartPie),
            ("Logs", IconChar.List),
            ("Alerts", IconChar.Bell),
            ("Incidents", IconChar.ExclamationTriangle),
            ("Threat Intel", IconChar.Biohazard),
            ("Users", IconChar.Users),
            ("Reports", IconChar.FileAlt),
            ("Audit Logs", IconChar.ClipboardList),
            ("Settings", IconChar.Cog)
        };

        var y = 10;
        foreach (var (name, icon) in navItems)
        {
            if (name == "Users" && !SessionManager.IsAdmin) continue;
            if (name == "Audit Logs" && !SessionManager.IsAdmin) continue;

            var btn = UiHelper.CreateSidebarButton(name, icon, new Point(0, y));
            btn.Width = 210;
            var navName = name;
            btn.Click += (_, _) => OnNavClick(navName, btn);
            navPanel.Controls.Add(btn);
            _navButtons.Add(btn);
            y += 52;
        }

        sidebar.Controls.Add(navPanel);
        sidebar.Controls.Add(logo);

        var topBar = new Guna2Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            FillColor = ThemeColors.Card,
            BorderColor = ThemeColors.Border,
            BorderThickness = 1
        };

        _lblPageTitle.Font = new Font("Segoe UI Semibold", 14F);
        _lblPageTitle.ForeColor = ThemeColors.Text;
        _lblPageTitle.Location = new Point(20, 18);
        _lblPageTitle.AutoSize = true;
        _lblPageTitle.Text = "Dashboard";

        _searchBox.PlaceholderText = "Search everywhere...";
        _searchBox.Size = new Size(280, 36);
        _searchBox.Location = new Point(250, 12);
        _searchBox.BorderRadius = 8;
        _searchBox.FillColor = Color.FromArgb(15, 23, 42);
        _searchBox.ForeColor = ThemeColors.Text;
        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
                await GlobalSearchAsync();
        };

        _lblUser.ForeColor = ThemeColors.TextMuted;
        _lblUser.AutoSize = true;
        _lblUser.Text = SessionManager.CurrentUser?.FullName ?? "";
        _lblUser.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        _lblLiveStatus.Font = new Font("Segoe UI Semibold", 9F);
        _lblLiveStatus.AutoSize = true;
        _lblLiveStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var btnLogout = UiHelper.CreateButton("Logout", IconChar.SignOutAlt, new Point(0, 10), new Size(110, 36));
        btnLogout.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnLogout.Click += async (_, _) =>
        {
            _simulator.Stop();
            await _auth.LogoutAsync();
            Application.Restart();
        };

        topBar.Controls.AddRange([_lblPageTitle, _searchBox, _lblLiveStatus, _lblUser, btnLogout]);
        topBar.Resize += (_, _) =>
        {
            _lblLiveStatus.Location = new Point(topBar.Width - 520, 18);
            _lblUser.Location = new Point(topBar.Width - 280, 20);
            btnLogout.Location = new Point(topBar.Width - 130, 12);
        };
        _lblLiveStatus.Location = new Point(topBar.Width - 520, 18);
        _lblUser.Location = new Point(topBar.Width - 280, 20);
        btnLogout.Location = new Point(topBar.Width - 130, 12);

        _contentPanel.Dock = DockStyle.Fill;
        _contentPanel.BackColor = ThemeColors.Background;
        _contentPanel.Padding = new Padding(20);

        Controls.Add(_contentPanel);
        Controls.Add(topBar);
        Controls.Add(sidebar);
    }

    private void OnNavClick(string name, Guna2Button btn)
    {
        UiHelper.SetActiveSidebarButton(btn, _navButtons);
        _lblPageTitle.Text = name;

        UserControl view = name switch
        {
            "Dashboard" => new DashboardView(_dashboard),
            "Logs" => new LogsView(_logs),
            "Alerts" => new AlertsView(_alerts),
            "Incidents" => new IncidentsView(_incidents),
            "Threat Intel" => new ThreatIntelView(_threatIntel),
            "Users" => new UsersView(_users),
            "Reports" => new ReportsView(_reports),
            "Audit Logs" => new AuditLogsView(_audit),
            "Settings" => new SettingsView(_settings, _audit, _liveData),
            _ => new DashboardView(_dashboard)
        };

        NavigateTo(name, view);
    }

    private void UpdateLiveStatusBadge()
    {
        var snap = _liveData.LastSnapshot;
        if (snap?.IsOnline == true)
        {
            _lblLiveStatus.Text = $"● LIVE  {snap.MaliciousIps.Count} IPs | {snap.Domains.Count} domains | CISA KEV";
            _lblLiveStatus.ForeColor = ThemeColors.Success;
        }
        else
        {
            _lblLiveStatus.Text = "● OFFLINE — sample/simulated data";
            _lblLiveStatus.ForeColor = ThemeColors.Warning;
        }
    }

    private void NavigateTo(string _, UserControl view)
    {
        _currentView?.Dispose();
        _contentPanel.Controls.Clear();
        view.Dock = DockStyle.Fill;
        _contentPanel.Controls.Add(view);
        _currentView = view;
    }

    private async Task GlobalSearchAsync()
    {
        var query = _searchBox.Text.Trim();
        if (string.IsNullOrEmpty(query)) return;

        var results = await _search.SearchAsync(query);
        if (results.Count == 0)
        {
            MessageBox.Show("No results found.", "Search", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var msg = string.Join("\n", results.Take(15).Select(r => $"[{r.Type}] {r.Title}: {r.Description[..Math.Min(60, r.Description.Length)]}"));
        MessageBox.Show(msg, $"Search Results ({results.Count})", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void StartAutoRefresh()
    {
        _refreshTimer = new System.Windows.Forms.Timer { Interval = 10000 };
        _refreshTimer.Tick += (_, _) =>
        {
            if (_currentView is DashboardView dv)
                dv.RefreshData();
        };
        _refreshTimer.Start();
    }

    private void ShowNotification(string title, string message, string severity)
    {
        if (InvokeRequired)
        {
            Invoke(() => ShowNotification(title, message, severity));
            return;
        }

        var toast = new Guna2Panel
        {
            Size = new Size(360, 90),
            FillColor = ThemeColors.Card,
            BorderRadius = 10,
            BorderColor = UiHelper.SeverityColor(severity),
            BorderThickness = 2,
            Location = new Point(Width - 400, Height - 150)
        };

        toast.Controls.Add(new Label
        {
            Text = title,
            ForeColor = UiHelper.SeverityColor(severity),
            Font = new Font("Segoe UI Semibold", 10F),
            Location = new Point(15, 12),
            AutoSize = true
        });
        toast.Controls.Add(new Label
        {
            Text = message.Length > 80 ? message[..80] + "..." : message,
            ForeColor = ThemeColors.TextMuted,
            Location = new Point(15, 38),
            Size = new Size(330, 40)
        });

        Controls.Add(toast);
        toast.BringToFront();

        var fadeTimer = new System.Windows.Forms.Timer { Interval = 5000 };
        fadeTimer.Tick += (_, _) =>
        {
            Controls.Remove(toast);
            toast.Dispose();
            fadeTimer.Stop();
            fadeTimer.Dispose();
        };
        fadeTimer.Start();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _simulator.Stop();
        _refreshTimer?.Stop();
        base.OnFormClosing(e);
    }
}

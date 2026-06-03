using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Services;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Views.Modules;

public class ReportsView : UserControl
{
    private readonly ReportController _controller;

    public ReportsView(ReportController controller)
    {
        _controller = controller;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        var panel = UiHelper.CreateCard(600, 400);
        panel.Location = new Point(20, 20);
        panel.Controls.Add(UiHelper.CreateTitleLabel("Generate Reports", 20, 20, 14));

        var reports = new (string Name, string Format, Func<string, Task> Export)[]
        {
            ("Security Report", "PDF", _controller.ExportSecurityPdfAsync),
            ("Incident Report", "Excel", _controller.ExportIncidentExcelAsync),
            ("Alert Report", "CSV", _controller.ExportAlertCsvAsync),
            ("Threat Intelligence Report", "PDF", _controller.ExportThreatIntelPdfAsync)
        };

        var y = 60;
        foreach (var (name, format, export) in reports)
        {
            panel.Controls.Add(new Label
            {
                Text = $"{name} ({format})",
                ForeColor = ThemeColors.Text,
                Location = new Point(20, y),
                AutoSize = true
            });

            var btn = UiHelper.CreateButton($"Export {format}", FontAwesome.Sharp.IconChar.Download,
                new Point(350, y - 5), new Size(120, 36));
            var exportFn = export;
            btn.Click += async (_, _) => await ExportAsync(name, format, exportFn);
            panel.Controls.Add(btn);
            y += 55;
        }

        Controls.Add(panel);
    }

    private static async Task ExportAsync(string name, string format, Func<string, Task> export)
    {
        var filter = format switch
        {
            "PDF" => "PDF|*.pdf",
            "Excel" => "Excel|*.xlsx",
            _ => "CSV|*.csv"
        };
        var ext = format switch { "PDF" => ".pdf", "Excel" => ".xlsx", _ => ".csv" };

        using var sfd = new SaveFileDialog
        {
            Filter = filter,
            FileName = $"{name.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}{ext}"
        };

        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            await export(sfd.FileName);
            MessageBox.Show($"{name} exported successfully.", "Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

public class SettingsView : UserControl
{
    private readonly SettingsController _controller;
    private readonly AuditController _audit;
    private readonly LiveDataController? _liveData;
    private readonly Label _lblLiveStatus = new();
    private readonly Guna2ComboBox _cmbTheme = new();
    private readonly Guna2TextBox _txtBruteForce = new();
    private readonly Guna2TextBox _txtPortScan = new();
    private readonly Guna2ToggleSwitch _toggleNotifications = new();

    public SettingsView(SettingsController controller, AuditController audit, LiveDataController? liveData = null)
    {
        _controller = controller;
        _audit = audit;
        _liveData = liveData;
        InitializeComponent();
        _ = LoadSettingsAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        var panel = UiHelper.CreateCard(550, 520);
        panel.Location = new Point(20, 20);
        panel.Controls.Add(UiHelper.CreateTitleLabel("Application Settings", 20, 20, 14));

        panel.Controls.Add(UiHelper.CreateTitleLabel("Live Threat Feeds", 20, 55, 11));
        _lblLiveStatus.ForeColor = ThemeColors.TextMuted;
        _lblLiveStatus.Location = new Point(20, 78);
        _lblLiveStatus.Size = new Size(500, 60);
        panel.Controls.Add(_lblLiveStatus);

        var btnSyncLive = UiHelper.CreateButton("Sync Live Feeds", FontAwesome.Sharp.IconChar.Sync, new Point(20, 140), new Size(160, 36));
        btnSyncLive.Click += async (_, _) => await SyncLiveFeedsAsync();
        panel.Controls.Add(btnSyncLive);

        AddSettingLabel("Theme (Dark/Light)", 190, panel);
        _cmbTheme.Location = new Point(20, 210);
        _cmbTheme.Size = new Size(200, 36);
        _cmbTheme.Items.AddRange(["Dark", "Light"]);
        panel.Controls.Add(_cmbTheme);

        AddSettingLabel("Brute Force Threshold", 260, panel);
        _txtBruteForce.Location = new Point(20, 280);
        _txtBruteForce.Size = new Size(100, 36);
        _txtBruteForce.BorderRadius = 6;
        _txtBruteForce.FillColor = Color.FromArgb(15, 23, 42);
        _txtBruteForce.ForeColor = ThemeColors.Text;
        panel.Controls.Add(_txtBruteForce);

        AddSettingLabel("Port Scan Threshold", 330, panel);
        _txtPortScan.Location = new Point(20, 350);
        _txtPortScan.Size = new Size(100, 36);
        _txtPortScan.BorderRadius = 6;
        _txtPortScan.FillColor = Color.FromArgb(15, 23, 42);
        _txtPortScan.ForeColor = ThemeColors.Text;
        panel.Controls.Add(_txtPortScan);

        AddSettingLabel("Enable Notifications", 410, panel);
        _toggleNotifications.Location = new Point(20, 430);
        _toggleNotifications.CheckedState.FillColor = ThemeColors.Primary;
        panel.Controls.Add(_toggleNotifications);

        var btnSave = UiHelper.CreateButton("Save Settings", FontAwesome.Sharp.IconChar.Save, new Point(20, 470), new Size(160, 40));
        btnSave.Click += async (_, _) => await SaveSettingsAsync();
        panel.Controls.Add(btnSave);

        var btnBackup = UiHelper.CreateButton("Backup Data", FontAwesome.Sharp.IconChar.Database, new Point(190, 470), new Size(160, 40));
        btnBackup.Click += async (_, _) => await BackupAsync();
        panel.Controls.Add(btnBackup);

        var auditPanel = UiHelper.CreateCard(550, 300);
        auditPanel.Location = new Point(590, 20);
        auditPanel.Controls.Add(UiHelper.CreateTitleLabel("Recent Audit Logs", 20, 20, 14));

        var auditGrid = new DataGridView { Location = new Point(10, 50), Size = new Size(520, 230), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom };
        UiHelper.StyleDataGridView(auditGrid);
        auditGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { HeaderText = "User", FillWeight = 15 },
            new DataGridViewTextBoxColumn { HeaderText = "Action", FillWeight = 15 },
            new DataGridViewTextBoxColumn { HeaderText = "Entity", FillWeight = 15 },
            new DataGridViewTextBoxColumn { HeaderText = "Details", FillWeight = 35 },
            new DataGridViewTextBoxColumn { HeaderText = "Time", FillWeight = 20 });
        auditPanel.Controls.Add(auditGrid);

        Controls.Add(auditPanel);
        Controls.Add(panel);

        LoadAuditLogsAsync(auditGrid);
    }

    private static void AddSettingLabel(string text, int y, Control parent)
    {
        parent.Controls.Add(new Label { Text = text, ForeColor = ThemeColors.TextMuted, Location = new Point(20, y), AutoSize = true });
    }

    private void AddSettingLabel(string text, int y) =>
        Controls.Add(new Label { Text = text, ForeColor = ThemeColors.TextMuted, Location = new Point(40, y), AutoSize = true });

    private async Task LoadSettingsAsync()
    {
        _cmbTheme.SelectedItem = await _controller.GetAsync("Theme") ?? "Dark";
        _txtBruteForce.Text = await _controller.GetAsync("BruteForceThreshold") ?? "5";
        _txtPortScan.Text = await _controller.GetAsync("PortScanThreshold") ?? "20";
        _toggleNotifications.Checked = (await _controller.GetAsync("NotificationsEnabled")) != "false";
        await UpdateLiveStatusLabelAsync();
    }

    private async Task UpdateLiveStatusLabelAsync()
    {
        var status = await _controller.GetAsync("LiveDataStatus") ?? "Unknown";
        var lastSync = await _controller.GetAsync("LiveDataLastSync");
        var ipCount = await _controller.GetAsync("LiveDataIpCount") ?? "0";
        var domainCount = await _controller.GetAsync("LiveDataDomainCount") ?? "0";

        _lblLiveStatus.Text = lastSync != null && DateTime.TryParse(lastSync, out var dt)
            ? $"Status: {status}\nLast sync: {dt.ToLocalTime():g}\nReal IPs: {ipCount} | Real domains: {domainCount}\nSources: Feodo Tracker, Spamhaus DROP, URLhaus, CISA KEV"
            : $"Status: {status}\nClick 'Sync Live Feeds' to fetch real threat intelligence.";
    }

    private async Task SyncLiveFeedsAsync()
    {
        if (_liveData == null) return;
        try
        {
            var snap = await _liveData.SyncAsync();
            await UpdateLiveStatusLabelAsync();
            MessageBox.Show(
                snap.IsOnline
                    ? $"Live sync complete!\n{snap.MaliciousIps.Count} malicious IPs\n{snap.Domains.Count} malicious domains\n{snap.SecurityEvents.Count} CISA KEV entries"
                    : $"Sync failed: {snap.ErrorMessage ?? "No data returned"}. Using offline mode.",
                "Live Feeds", MessageBoxButtons.OK,
                snap.IsOnline ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Sync error: {ex.Message}", "Live Feeds", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task SaveSettingsAsync()
    {
        await _controller.SetAsync("Theme", _cmbTheme.SelectedItem?.ToString() ?? "Dark", "UI");
        await _controller.SetAsync("BruteForceThreshold", _txtBruteForce.Text, "Alerts");
        await _controller.SetAsync("PortScanThreshold", _txtPortScan.Text, "Alerts");
        await _controller.SetAsync("NotificationsEnabled", _toggleNotifications.Checked.ToString().ToLower(), "Notifications");

        if (_cmbTheme.SelectedItem?.ToString() == "Light")
            ThemeColors.ApplyLightTheme();
        else
            ThemeColors.ApplyDarkTheme();

        MessageBox.Show("Settings saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task BackupAsync()
    {
        using var sfd = new SaveFileDialog { Filter = "JSON Backup|*.json", FileName = $"CyberWatch_Backup_{DateTime.Now:yyyyMMdd}.json" };
        if (sfd.ShowDialog() != DialogResult.OK) return;

        try
        {
            await _controller.BackupAsync(sfd.FileName);
            MessageBox.Show("Backup completed.", "Backup", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void LoadAuditLogsAsync(DataGridView grid)
    {
        var logs = await _audit.GetAllAsync();
        grid.Rows.Clear();
        foreach (var log in logs.OrderByDescending(l => l.Timestamp).Take(50))
        {
            grid.Rows.Add(log.Username, log.Action, log.EntityType, log.Details,
                log.Timestamp.ToLocalTime().ToString("g"));
        }
    }
}

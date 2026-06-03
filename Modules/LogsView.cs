using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Views.Modules;

public class LogsView : UserControl
{
    private readonly LogController _controller;
    private readonly DataGridView _grid = new();
    private readonly Guna2TextBox _txtSearch = new();
    private readonly Guna2ComboBox _cmbSeverity = new();
    private readonly Guna2ComboBox _cmbEventType = new();
    private readonly Guna2TextBox _txtUsername = new();
    private readonly Guna2TextBox _txtSourceIp = new();
    private List<LogEntry> _currentLogs = [];

    public LogsView(LogController controller)
    {
        _controller = controller;
        InitializeComponent();
        _ = LoadLogsAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        var filterPanel = UiHelper.CreateCard(1100, 120);
        filterPanel.Dock = DockStyle.Top;
        filterPanel.Margin = new Padding(0, 0, 0, 15);

        _txtSearch.PlaceholderText = "Search logs...";
        _txtSearch.Location = new Point(15, 15);
        _txtSearch.Size = new Size(200, 36);
        StyleInput(_txtSearch);

        _cmbSeverity.Location = new Point(225, 15);
        _cmbSeverity.Size = new Size(120, 36);
        _cmbSeverity.Items.AddRange(["All", "Critical", "High", "Medium", "Low"]);
        _cmbSeverity.SelectedIndex = 0;
        StyleCombo(_cmbSeverity);

        _cmbEventType.Location = new Point(355, 15);
        _cmbEventType.Size = new Size(140, 36);
        _cmbEventType.Items.AddRange(["All", "Login", "Failed Login", "Port Scan", "Malware", "Network Activity"]);
        _cmbEventType.SelectedIndex = 0;
        StyleCombo(_cmbEventType);

        _txtUsername.PlaceholderText = "Username";
        _txtUsername.Location = new Point(505, 15);
        _txtUsername.Size = new Size(120, 36);
        StyleInput(_txtUsername);

        _txtSourceIp.PlaceholderText = "Source IP";
        _txtSourceIp.Location = new Point(635, 15);
        _txtSourceIp.Size = new Size(130, 36);
        StyleInput(_txtSourceIp);

        var btnSearch = UiHelper.CreateButton("Filter", FontAwesome.Sharp.IconChar.Filter, new Point(15, 65), new Size(100, 36));
        btnSearch.Click += async (_, _) => await LoadLogsAsync();

        var btnImport = UiHelper.CreateButton("Import", FontAwesome.Sharp.IconChar.FileImport, new Point(125, 65), new Size(100, 36));
        btnImport.Click += async (_, _) => await ImportAsync();

        var btnExport = UiHelper.CreateButton("Export CSV", FontAwesome.Sharp.IconChar.FileExport, new Point(235, 65), new Size(120, 36));
        btnExport.Click += async (_, _) => await ExportAsync();

        var btnAdd = UiHelper.CreateButton("Add Log", FontAwesome.Sharp.IconChar.Plus, new Point(365, 65), new Size(100, 36));
        btnAdd.Click += async (_, _) => await AddLogAsync();

        var btnDelete = UiHelper.CreateButton("Delete", FontAwesome.Sharp.IconChar.Trash, new Point(475, 65), new Size(100, 36));
        btnDelete.FillColor = ThemeColors.Danger;
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync();

        var btnEdit = UiHelper.CreateButton("Edit", FontAwesome.Sharp.IconChar.Edit, new Point(585, 65), new Size(100, 36));
        btnEdit.Click += async (_, _) => await EditSelectedAsync();

        filterPanel.Controls.AddRange([_txtSearch, _cmbSeverity, _cmbEventType, _txtUsername, _txtSourceIp,
            btnSearch, btnImport, btnExport, btnAdd, btnDelete, btnEdit]);

        _grid.Dock = DockStyle.Fill;
        _grid.CellDoubleClick += async (_, _) => await EditSelectedAsync();
        UiHelper.StyleDataGridView(_grid);
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", HeaderText = "ID", Visible = false },
            new DataGridViewTextBoxColumn { Name = "EventId", HeaderText = "Event ID" },
            new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "Timestamp" },
            new DataGridViewTextBoxColumn { Name = "SourceIP", HeaderText = "Source IP" },
            new DataGridViewTextBoxColumn { Name = "DestIP", HeaderText = "Dest IP" },
            new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "User" },
            new DataGridViewTextBoxColumn { Name = "EventType", HeaderText = "Type" },
            new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Severity" },
            new DataGridViewTextBoxColumn { Name = "Mitre", HeaderText = "MITRE ATT&CK" },
            new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "Threat Score" },
            new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Message" });

        Controls.Add(_grid);
        Controls.Add(filterPanel);
    }

    private static void StyleInput(Guna2TextBox tb)
    {
        tb.BorderRadius = 6;
        tb.FillColor = Color.FromArgb(15, 23, 42);
        tb.ForeColor = ThemeColors.Text;
        tb.BorderColor = ThemeColors.Border;
    }

    private static void StyleCombo(Guna2ComboBox cb)
    {
        cb.BorderRadius = 6;
        cb.FillColor = Color.FromArgb(15, 23, 42);
        cb.ForeColor = ThemeColors.Text;
        cb.BorderColor = ThemeColors.Border;
    }

    private async Task LoadLogsAsync()
    {
        var severity = _cmbSeverity.SelectedItem?.ToString() == "All" ? null : _cmbSeverity.SelectedItem?.ToString();
        var eventType = _cmbEventType.SelectedItem?.ToString() == "All" ? null : _cmbEventType.SelectedItem?.ToString();

        _currentLogs = await _controller.SearchAsync(
            _txtSearch.Text, severity, eventType,
            _txtUsername.Text, _txtSourceIp.Text, null, null);

        _grid.Rows.Clear();
        foreach (var log in _currentLogs)
        {
            var idx = _grid.Rows.Add(log.LogId, log.EventId, log.Timestamp.ToLocalTime().ToString("g"),
                log.SourceIP, log.DestinationIP, log.Username, log.EventType, log.Severity,
                log.MitreTechnique ?? "-", log.ThreatScore, log.Message);
            _grid.Rows[idx].Cells["Severity"].Style.ForeColor = UiHelper.SeverityColor(log.Severity);
        }
    }

    private async Task ImportAsync()
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Log Files|*.csv;*.json;*.txt|All Files|*.*"
        };
        if (ofd.ShowDialog() != DialogResult.OK) return;

        try
        {
            var count = await _controller.ImportAsync(ofd.FileName);
            MessageBox.Show($"Imported {count} logs.", "Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadLogsAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task ExportAsync()
    {
        using var sfd = new SaveFileDialog { Filter = "CSV|*.csv", FileName = $"logs_{DateTime.Now:yyyyMMdd}.csv" };
        if (sfd.ShowDialog() != DialogResult.OK) return;
        await _controller.ExportCsvAsync(sfd.FileName, _currentLogs);
        MessageBox.Show("Export complete.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private async Task AddLogAsync()
    {
        using var form = new LogEditForm(null);
        if (form.ShowDialog() == DialogResult.OK && form.LogEntry != null)
        {
            await _controller.AddAsync(form.LogEntry);
            await LoadLogsAsync();
        }
    }

    private async Task EditSelectedAsync()
    {
        if (_grid.SelectedRows.Count == 0) return;
        var id = (int)_grid.SelectedRows[0].Cells["Id"].Value;
        var log = await _controller.GetByIdAsync(id);
        if (log == null) return;

        using var form = new LogEditForm(log);
        if (form.ShowDialog() == DialogResult.OK && form.LogEntry != null)
        {
            await _controller.UpdateAsync(form.LogEntry);
            await LoadLogsAsync();
        }
    }

    private async Task DeleteSelectedAsync()
    {
        if (_grid.SelectedRows.Count == 0) return;
        if (MessageBox.Show("Delete selected log?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        var id = (int)_grid.SelectedRows[0].Cells["Id"].Value;
        await _controller.DeleteAsync(id);
        await LoadLogsAsync();
    }
}

public class LogEditForm : Form
{
    public LogEntry? LogEntry { get; private set; }

    public LogEditForm(LogEntry? existing)
    {
        Text = existing == null ? "Add Log" : "Edit Log";
        Size = new Size(450, 480);
        StartPosition = FormStartPosition.CenterParent;
        UiHelper.StyleForm(this);

        var fields = new Dictionary<string, Guna2TextBox>();
        var labels = new[] { "EventId", "SourceIP", "DestIP", "Username", "EventType", "Severity", "Message" };
        var y = 20;

        foreach (var label in labels)
        {
            Controls.Add(new Label { Text = label, ForeColor = ThemeColors.TextMuted, Location = new Point(20, y), AutoSize = true });
            var tb = new Guna2TextBox { Location = new Point(20, y + 20), Size = new Size(390, 36), BorderRadius = 6,
                FillColor = ThemeColors.Card, ForeColor = ThemeColors.Text };
            fields[label] = tb;
            Controls.Add(tb);
            y += 65;
        }

        if (existing != null)
        {
            fields["EventId"].Text = existing.EventId;
            fields["SourceIP"].Text = existing.SourceIP;
            fields["DestIP"].Text = existing.DestinationIP;
            fields["Username"].Text = existing.Username;
            fields["EventType"].Text = existing.EventType;
            fields["Severity"].Text = existing.Severity;
            fields["Message"].Text = existing.Message;
        }

        var btnSave = UiHelper.CreateButton("Save", FontAwesome.Sharp.IconChar.Save, new Point(20, y + 10), new Size(390, 40));
        btnSave.Click += (_, _) =>
        {
            LogEntry = existing ?? new LogEntry();
            LogEntry.EventId = fields["EventId"].Text;
            LogEntry.Timestamp = DateTime.UtcNow;
            LogEntry.SourceIP = fields["SourceIP"].Text;
            LogEntry.DestinationIP = fields["DestIP"].Text;
            LogEntry.Username = fields["Username"].Text;
            LogEntry.EventType = fields["EventType"].Text;
            LogEntry.Severity = fields["Severity"].Text;
            LogEntry.Message = fields["Message"].Text;
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(btnSave);
    }
}

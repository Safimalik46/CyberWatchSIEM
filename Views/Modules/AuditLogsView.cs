using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Views.Modules;

public class AuditLogsView : UserControl
{
    private readonly AuditController _controller;
    private readonly DataGridView _grid = new();
    private readonly Guna2TextBox _txtSearch = new();

    public AuditLogsView(AuditController controller)
    {
        _controller = controller;
        InitializeComponent();
        _ = LoadLogsAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        var toolbar = UiHelper.CreateCard(1100, 60);
        toolbar.Dock = DockStyle.Top;
        toolbar.Margin = new Padding(0, 0, 0, 15);

        _txtSearch.PlaceholderText = "Search audit logs...";
        _txtSearch.Location = new Point(15, 12);
        _txtSearch.Size = new Size(280, 36);
        _txtSearch.BorderRadius = 6;
        _txtSearch.FillColor = Color.FromArgb(15, 23, 42);
        _txtSearch.ForeColor = ThemeColors.Text;

        var btnRefresh = UiHelper.CreateButton("Refresh", FontAwesome.Sharp.IconChar.Sync, new Point(310, 12), new Size(100, 36));
        btnRefresh.Click += async (_, _) => await LoadLogsAsync();

        toolbar.Controls.AddRange([_txtSearch, btnRefresh]);

        _grid.Dock = DockStyle.Fill;
        UiHelper.StyleDataGridView(_grid);
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "User" },
            new DataGridViewTextBoxColumn { Name = "Action", HeaderText = "Action" },
            new DataGridViewTextBoxColumn { Name = "Entity", HeaderText = "Entity Type" },
            new DataGridViewTextBoxColumn { Name = "Details", HeaderText = "Details" },
            new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Timestamp" });

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    private async Task LoadLogsAsync()
    {
        var logs = await _controller.GetAllAsync();
        var search = _txtSearch.Text.Trim().ToLower();

        if (!string.IsNullOrEmpty(search))
            logs = logs.Where(l =>
                l.Username.ToLower().Contains(search) ||
                l.Action.ToLower().Contains(search) ||
                l.Details.ToLower().Contains(search)).ToList();

        _grid.Rows.Clear();
        foreach (var log in logs.OrderByDescending(l => l.Timestamp).Take(200))
        {
            _grid.Rows.Add(log.Username, log.Action, log.EntityType, log.Details,
                log.Timestamp.ToLocalTime().ToString("g"));
        }
    }
}

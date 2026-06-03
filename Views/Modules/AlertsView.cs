
using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Views.Modules;

public class AlertsView : UserControl
{
    private readonly AlertController _controller;
    private readonly DataGridView _grid = new();
    private readonly Guna2TextBox _txtSearch = new();
    private readonly Guna2ComboBox _cmbSeverity = new();
    private readonly Guna2ComboBox _cmbStatus = new();

    public AlertsView(AlertController controller)
    {
        _controller = controller;
        InitializeComponent();
        _ = LoadAlertsAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        var filterPanel = UiHelper.CreateCard(1100, 70);
        filterPanel.Dock = DockStyle.Top;
        filterPanel.Margin = new Padding(0, 0, 0, 15);

        _txtSearch.PlaceholderText = "Search alerts...";
        _txtSearch.Location = new Point(15, 17);
        _txtSearch.Size = new Size(250, 36);
        _txtSearch.BorderRadius = 6;
        _txtSearch.FillColor = Color.FromArgb(15, 23, 42);
        _txtSearch.ForeColor = ThemeColors.Text;

        _cmbSeverity.Location = new Point(280, 17);
        _cmbSeverity.Size = new Size(120, 36);
        _cmbSeverity.Items.AddRange(["All", "Critical", "High", "Medium", "Low"]);
        _cmbSeverity.SelectedIndex = 0;

        _cmbStatus.Location = new Point(410, 17);
        _cmbStatus.Size = new Size(120, 36);
        _cmbStatus.Items.AddRange(["All", "New", "Acknowledged", "Resolved"]);
        _cmbStatus.SelectedIndex = 0;

        var btnFilter = UiHelper.CreateButton("Filter", FontAwesome.Sharp.IconChar.Filter, new Point(545, 15), new Size(100, 36));
        btnFilter.Click += async (_, _) => await LoadAlertsAsync();

        var btnAck = UiHelper.CreateButton("Acknowledge", FontAwesome.Sharp.IconChar.Check, new Point(655, 15), new Size(130, 36));
        btnAck.Click += async (_, _) => await UpdateStatusAsync("Acknowledged");

        filterPanel.Controls.AddRange([_txtSearch, _cmbSeverity, _cmbStatus, btnFilter, btnAck]);

        _grid.Dock = DockStyle.Fill;
        UiHelper.StyleDataGridView(_grid);
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", Visible = false },
            new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Alert Code" },
            new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type" },
            new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Severity" },
            new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time" },
            new DataGridViewTextBoxColumn { Name = "SourceIP", HeaderText = "Source IP" },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status" },
            new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description" });

        Controls.Add(_grid);
        Controls.Add(filterPanel);
    }

    private async Task LoadAlertsAsync()
    {
        var sev = _cmbSeverity.SelectedItem?.ToString() == "All" ? null : _cmbSeverity.SelectedItem?.ToString();
        var st = _cmbStatus.SelectedItem?.ToString() == "All" ? null : _cmbStatus.SelectedItem?.ToString();
        var alerts = await _controller.SearchAsync(_txtSearch.Text, sev, st);

        _grid.Rows.Clear();
        foreach (var a in alerts)
        {
            var idx = _grid.Rows.Add(a.AlertId, a.AlertCode, a.AlertType, a.Severity,
                a.Time.ToLocalTime().ToString("g"), a.SourceIP, a.Status, a.Description);
            _grid.Rows[idx].Cells["Severity"].Style.ForeColor = UiHelper.SeverityColor(a.Severity);
        }
    }

    private async Task UpdateStatusAsync(string status)
    {
        if (_grid.SelectedRows.Count == 0) return;
        var id = (int)_grid.SelectedRows[0].Cells["Id"].Value;
        await _controller.UpdateStatusAsync(id, status);
        await LoadAlertsAsync();
    }
}

public class IncidentsView : UserControl
{
    private readonly IncidentController _controller;
    private readonly DataGridView _grid = new();

    public IncidentsView(IncidentController controller)
    {
        _controller = controller;
        InitializeComponent();
        _ = LoadIncidentsAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        var toolbar = UiHelper.CreateCard(1100, 60);
        toolbar.Dock = DockStyle.Top;
        toolbar.Margin = new Padding(0, 0, 0, 15);

        var btnCreate = UiHelper.CreateButton("Create Incident", FontAwesome.Sharp.IconChar.Plus, new Point(15, 12), new Size(150, 36));
        btnCreate.Click += async (_, _) => await CreateIncidentAsync();

        var btnAssign = UiHelper.CreateButton("Assign", FontAwesome.Sharp.IconChar.UserTag, new Point(175, 12), new Size(100, 36));
        btnAssign.Click += async (_, _) => await UpdateIncidentAsync();

        toolbar.Controls.AddRange([btnCreate, btnAssign]);

        _grid.Dock = DockStyle.Fill;
        UiHelper.StyleDataGridView(_grid);
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", Visible = false },
            new DataGridViewTextBoxColumn { Name = "Code", HeaderText = "Code" },
            new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Alert Name" },
            new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Severity" },
            new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status" },
            new DataGridViewTextBoxColumn { Name = "Assigned", HeaderText = "Assigned To" },
            new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description" });

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    private async Task LoadIncidentsAsync()
    {
        var incidents = await _controller.GetAllAsync();
        _grid.Rows.Clear();
        foreach (var i in incidents)
        {
            var idx = _grid.Rows.Add(i.IncidentId, i.IncidentCode, i.AlertName, i.Severity,
                i.Status, i.AssignedTo, i.Description);
            _grid.Rows[idx].Cells["Severity"].Style.ForeColor = UiHelper.SeverityColor(i.Severity);
        }
    }

    private async Task CreateIncidentAsync()
    {
        using var form = new IncidentEditForm();
        if (form.ShowDialog() == DialogResult.OK && form.Incident != null)
        {
            await _controller.CreateAsync(form.Incident);
            await LoadIncidentsAsync();
        }
    }

    private async Task UpdateIncidentAsync()
    {
        if (_grid.SelectedRows.Count == 0) return;
        var id = (int)_grid.SelectedRows[0].Cells["Id"].Value;
        var incidents = await _controller.GetAllAsync();
        var incident = incidents.FirstOrDefault(i => i.IncidentId == id);
        if (incident == null) return;

        using var form = new IncidentEditForm(incident);
        if (form.ShowDialog() == DialogResult.OK && form.Incident != null)
        {
            await _controller.UpdateAsync(form.Incident);
            await LoadIncidentsAsync();
        }
    }
}

public class IncidentEditForm : Form
{
    public Models.Incident? Incident { get; private set; }

    public IncidentEditForm(Models.Incident? existing = null)
    {
        Text = "Incident";
        Size = new Size(450, 420);
        StartPosition = FormStartPosition.CenterParent;
        UiHelper.StyleForm(this);

        var txtName = AddField("Alert Name", 20, existing?.AlertName ?? "");
        var cmbSev = AddCombo("Severity", 85, ["Critical", "High", "Medium", "Low"], existing?.Severity ?? "Medium");
        var cmbStatus = AddCombo("Status", 150, ["Open", "Investigating", "Resolved", "Closed"], existing?.Status ?? "Open");
        var txtAssigned = AddField("Assigned To", 215, existing?.AssignedTo ?? SessionManager.CurrentUser?.Username ?? "");
        var txtDesc = AddField("Description", 280, existing?.Description ?? "");
        var txtRes = AddField("Resolution", 345, existing?.Resolution ?? "");

        var btnSave = UiHelper.CreateButton("Save", FontAwesome.Sharp.IconChar.Save, new Point(20, 410), new Size(390, 40));
        btnSave.Click += (_, _) =>
        {
            Incident = existing ?? new Models.Incident();
            Incident.AlertName = txtName.Text;
            Incident.Severity = cmbSev.SelectedItem?.ToString() ?? "Medium";
            Incident.Status = cmbStatus.SelectedItem?.ToString() ?? "Open";
            Incident.AssignedTo = txtAssigned.Text;
            Incident.Description = txtDesc.Text;
            Incident.Resolution = txtRes.Text;
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(btnSave);
        Height = 520;
    }

    private Guna2TextBox AddField(string label, int y, string value)
    {
        Controls.Add(new Label { Text = label, ForeColor = ThemeColors.TextMuted, Location = new Point(20, y), AutoSize = true });
        var tb = new Guna2TextBox { Location = new Point(20, y + 18), Size = new Size(390, 32), Text = value,
            BorderRadius = 6, FillColor = ThemeColors.Card, ForeColor = ThemeColors.Text };
        Controls.Add(tb);
        return tb;
    }

    private Guna2ComboBox AddCombo(string label, int y, string[] items, string selected)
    {
        Controls.Add(new Label { Text = label, ForeColor = ThemeColors.TextMuted, Location = new Point(20, y), AutoSize = true });
        var cb = new Guna2ComboBox { Location = new Point(20, y + 18), Size = new Size(390, 32) };
        cb.Items.AddRange(items);
        cb.SelectedItem = selected;
        Controls.Add(cb);
        return cb;
    }
}

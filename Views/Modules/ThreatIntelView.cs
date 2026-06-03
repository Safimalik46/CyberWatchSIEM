using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Views.Modules;

public class ThreatIntelView : UserControl
{
    private readonly ThreatIntelController _controller;
    private readonly TabControl _tabs = new();
    private DataGridView _ipGrid = new();
    private DataGridView _domainGrid = new();
    private DataGridView _sigGrid = new();

    public ThreatIntelView(ThreatIntelController controller)
    {
        _controller = controller;
        InitializeComponent();
        _ = LoadAllAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        _tabs.Dock = DockStyle.Fill;
        _tabs.Appearance = TabAppearance.FlatButtons;
        _tabs.ItemSize = new Size(0, 1);
        _tabs.SizeMode = TabSizeMode.Fixed;

        _ipGrid = CreateGrid(["Id", "IP Address", "Threat Type", "Severity", "Description"]);
        _domainGrid = CreateGrid(["Id", "Domain", "Category", "Severity", "Description"]);
        _sigGrid = CreateGrid(["Id", "Signature", "Family", "Severity", "Description"]);

        var ipPanel = CreateTabPanel("Malicious IPs", _ipGrid, async () => await ShowAddIPDialog());
        var domainPanel = CreateTabPanel("Suspicious Domains", _domainGrid, async () => await ShowAddDomainDialog());
        var sigPanel = CreateTabPanel("Malware Signatures", _sigGrid, async () => await ShowAddSigDialog());

        var tabBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 50, BackColor = Color.Transparent };
        var btnIPs = UiHelper.CreateButton("Malicious IPs", FontAwesome.Sharp.IconChar.NetworkWired, new Point(0, 5), new Size(160, 40));
        var btnDomains = UiHelper.CreateButton("Domains", FontAwesome.Sharp.IconChar.Globe, new Point(0, 5), new Size(120, 40));
        var btnSigs = UiHelper.CreateButton("Signatures", FontAwesome.Sharp.IconChar.Bug, new Point(0, 5), new Size(130, 40));

        btnIPs.Click += (_, _) => { _tabs.SelectedIndex = 0; HighlightTab(btnIPs, btnDomains, btnSigs); };
        btnDomains.Click += (_, _) => { _tabs.SelectedIndex = 1; HighlightTab(btnDomains, btnIPs, btnSigs); };
        btnSigs.Click += (_, _) => { _tabs.SelectedIndex = 2; HighlightTab(btnSigs, btnIPs, btnDomains); };

        tabBar.Controls.AddRange([btnIPs, btnDomains, btnSigs]);
        _tabs.TabPages.AddRange([ipPanel, domainPanel, sigPanel]);

        Controls.Add(_tabs);
        Controls.Add(tabBar);
    }

    private static void HighlightTab(Guna2Button active, Guna2Button b2, Guna2Button b3)
    {
        active.FillColor = ThemeColors.Primary;
        b2.FillColor = ThemeColors.Card;
        b3.FillColor = ThemeColors.Card;
    }

    private TabPage CreateTabPanel(string title, DataGridView grid, Func<Task> onAdd)
    {
        var page = new TabPage(title) { BackColor = ThemeColors.Background };
        var toolbar = UiHelper.CreateCard(1100, 55);
        toolbar.Dock = DockStyle.Top;
        toolbar.Margin = new Padding(0, 0, 0, 10);

        var btnAdd = UiHelper.CreateButton("Add", FontAwesome.Sharp.IconChar.Plus, new Point(15, 10), new Size(90, 36));
        btnAdd.Click += async (_, _) => await onAdd();

        var btnDelete = UiHelper.CreateButton("Delete", FontAwesome.Sharp.IconChar.Trash, new Point(115, 10), new Size(90, 36));
        btnDelete.FillColor = ThemeColors.Danger;
        btnDelete.Click += async (_, _) => await DeleteSelectedAsync(grid);

        toolbar.Controls.AddRange([btnAdd, btnDelete]);
        grid.Dock = DockStyle.Fill;
        page.Controls.Add(grid);
        page.Controls.Add(toolbar);
        return page;
    }

    private static DataGridView CreateGrid(string[] columns)
    {
        var grid = new DataGridView();
        UiHelper.StyleDataGridView(grid);
        foreach (var col in columns)
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = col.Replace(" ", ""), HeaderText = col,
                Visible = col != "Id" || col == "Id" });
        grid.Columns["Id"]!.Visible = false;
        return grid;
    }

    private async Task LoadAllAsync()
    {
        var ips = await _controller.GetIPsAsync();
        _ipGrid.Rows.Clear();
        foreach (var ip in ips)
            _ipGrid.Rows.Add(ip.Id, ip.IPAddress, ip.ThreatType, ip.Severity, ip.Description);

        var domains = await _controller.GetDomainsAsync();
        _domainGrid.Rows.Clear();
        foreach (var d in domains)
            _domainGrid.Rows.Add(d.Id, d.Domain, d.Category, d.Severity, d.Description);

        var sigs = await _controller.GetSignaturesAsync();
        _sigGrid.Rows.Clear();
        foreach (var s in sigs)
            _sigGrid.Rows.Add(s.Id, s.Signature, s.MalwareFamily, s.Severity, s.Description);
    }

    private async Task ShowAddIPDialog()
    {
        using var form = new SimpleInputForm("Add Malicious IP", ["IP Address", "Threat Type", "Severity", "Description"]);
        if (form.ShowDialog() == DialogResult.OK)
        {
            await _controller.AddIPAsync(new MaliciousIP
            {
                IPAddress = form.Values[0],
                ThreatType = form.Values[1],
                Severity = form.Values[2],
                Description = form.Values[3]
            });
            await LoadAllAsync();
        }
    }

    private async Task ShowAddDomainDialog()
    {
        using var form = new SimpleInputForm("Add Domain", ["Domain", "Category", "Severity", "Description"]);
        if (form.ShowDialog() == DialogResult.OK)
        {
            await _controller.AddDomainAsync(new SuspiciousDomain
            {
                Domain = form.Values[0],
                Category = form.Values[1],
                Severity = form.Values[2],
                Description = form.Values[3]
            });
            await LoadAllAsync();
        }
    }

    private async Task ShowAddSigDialog()
    {
        using var form = new SimpleInputForm("Add Signature", ["Signature", "Family", "Severity", "Description"]);
        if (form.ShowDialog() == DialogResult.OK)
        {
            await _controller.AddSignatureAsync(new MalwareSignature
            {
                Signature = form.Values[0],
                MalwareFamily = form.Values[1],
                Severity = form.Values[2],
                Description = form.Values[3]
            });
            await LoadAllAsync();
        }
    }

    private async Task DeleteSelectedAsync(DataGridView grid)
    {
        if (grid.SelectedRows.Count == 0) return;
        var id = (int)grid.SelectedRows[0].Cells["Id"].Value;

        if (grid == _ipGrid)
            await _controller.DeleteIPAsync(id);
        else if (grid == _domainGrid)
            await _controller.DeleteDomainAsync(id);
        else
            await _controller.DeleteSignatureAsync(id);

        await LoadAllAsync();
    }
}

public class SimpleInputForm : Form
{
    public string[] Values { get; private set; } = [];
    private readonly List<Guna2TextBox> _fields = [];

    public SimpleInputForm(string title, string[] labels)
    {
        Text = title;
        Size = new Size(400, 80 + labels.Length * 65);
        StartPosition = FormStartPosition.CenterParent;
        UiHelper.StyleForm(this);

        var y = 20;
        foreach (var label in labels)
        {
            Controls.Add(new Label { Text = label, ForeColor = ThemeColors.TextMuted, Location = new Point(20, y), AutoSize = true });
            var tb = new Guna2TextBox { Location = new Point(20, y + 20), Size = new Size(340, 32), BorderRadius = 6,
                FillColor = ThemeColors.Card, ForeColor = ThemeColors.Text };
            _fields.Add(tb);
            Controls.Add(tb);
            y += 60;
        }

        var btnSave = UiHelper.CreateButton("Save", FontAwesome.Sharp.IconChar.Save, new Point(20, y), new Size(340, 36));
        btnSave.Click += (_, _) =>
        {
            Values = _fields.Select(f => f.Text).ToArray();
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(btnSave);
    }
}

public class UsersView : UserControl
{
    private readonly Controllers.UserController _controller;
    private readonly DataGridView _grid = new();

    public UsersView(Controllers.UserController controller)
    {
        _controller = controller;
        InitializeComponent();
        _ = LoadUsersAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;

        var toolbar = UiHelper.CreateCard(1100, 60);
        toolbar.Dock = DockStyle.Top;
        toolbar.Margin = new Padding(0, 0, 0, 15);

        var btnAdd = UiHelper.CreateButton("Add User", FontAwesome.Sharp.IconChar.UserPlus, new Point(15, 12), new Size(120, 36));
        btnAdd.Click += async (_, _) => await AddUserAsync();

        var btnReset = UiHelper.CreateButton("Reset Password", FontAwesome.Sharp.IconChar.Key, new Point(145, 12), new Size(140, 36));
        btnReset.Click += async (_, _) => await ResetPasswordAsync();

        toolbar.Controls.AddRange([btnAdd, btnReset]);

        _grid.Dock = DockStyle.Fill;
        UiHelper.StyleDataGridView(_grid);
        _grid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Id", Visible = false },
            new DataGridViewTextBoxColumn { Name = "Username", HeaderText = "Username" },
            new DataGridViewTextBoxColumn { Name = "FullName", HeaderText = "Full Name" },
            new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "Email" },
            new DataGridViewTextBoxColumn { Name = "Role", HeaderText = "Role" },
            new DataGridViewTextBoxColumn { Name = "Active", HeaderText = "Active" },
            new DataGridViewTextBoxColumn { Name = "LastLogin", HeaderText = "Last Login" });

        Controls.Add(_grid);
        Controls.Add(toolbar);
    }

    private async Task LoadUsersAsync()
    {
        var users = await _controller.GetAllAsync();
        _grid.Rows.Clear();
        foreach (var u in users)
        {
            _grid.Rows.Add(u.UserId, u.Username, u.FullName, u.Email, u.Role,
                u.IsActive ? "Yes" : "No", u.LastLogin?.ToLocalTime().ToString("g") ?? "Never");
        }
    }

    private async Task AddUserAsync()
    {
        using var form = new UserEditForm();
        if (form.ShowDialog() == DialogResult.OK && form.User != null && form.Password != null)
        {
            var (ok, msg) = await _controller.CreateAsync(form.User, form.Password);
            MessageBox.Show(msg, ok ? "Success" : "Error", MessageBoxButtons.OK,
                ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
            if (ok) await LoadUsersAsync();
        }
    }

    private async Task ResetPasswordAsync()
    {
        if (_grid.SelectedRows.Count == 0) return;
        var id = (int)_grid.SelectedRows[0].Cells["Id"].Value;

        using var form = new SimpleInputForm("Reset Password", ["New Password"]);
        if (form.ShowDialog() != DialogResult.OK || form.Values.Length == 0) return;

        var (ok, msg) = await _controller.ResetPasswordAsync(id, form.Values[0]);
        MessageBox.Show(msg, ok ? "Success" : "Error", MessageBoxButtons.OK,
            ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }
}

public class UserEditForm : Form
{
    public User? User { get; private set; }
    public string? Password { get; private set; }

    public UserEditForm()
    {
        Text = "Add User";
        Size = new Size(400, 380);
        StartPosition = FormStartPosition.CenterParent;
        UiHelper.StyleForm(this);

        var txtUser = AddField("Username", 20);
        var txtPass = AddField("Password", 85);
        txtPass.UseSystemPasswordChar = true;
        var txtName = AddField("Full Name", 150);
        var txtEmail = AddField("Email", 215);
        var cmbRole = new Guna2ComboBox { Location = new Point(20, 298), Size = new Size(340, 32) };
        cmbRole.Items.AddRange(["Admin", "Analyst"]);
        cmbRole.SelectedIndex = 1;
        Controls.Add(new Label { Text = "Role", ForeColor = ThemeColors.TextMuted, Location = new Point(20, 280), AutoSize = true });
        Controls.Add(cmbRole);

        var btnSave = UiHelper.CreateButton("Save", FontAwesome.Sharp.IconChar.Save, new Point(20, 340), new Size(340, 36));
        btnSave.Click += (_, _) =>
        {
            User = new User
            {
                Username = txtUser.Text,
                FullName = txtName.Text,
                Email = txtEmail.Text,
                Role = cmbRole.SelectedItem?.ToString() ?? "Analyst"
            };
            Password = txtPass.Text;
            DialogResult = DialogResult.OK;
            Close();
        };
        Controls.Add(btnSave);
    }

    private Guna2TextBox AddField(string label, int y)
    {
        Controls.Add(new Label { Text = label, ForeColor = ThemeColors.TextMuted, Location = new Point(20, y), AutoSize = true });
        var tb = new Guna2TextBox { Location = new Point(20, y + 18), Size = new Size(340, 32), BorderRadius = 6,
            FillColor = ThemeColors.Card, ForeColor = ThemeColors.Text };
        Controls.Add(tb);
        return tb;
    }
}

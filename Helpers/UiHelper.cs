using FontAwesome.Sharp;
using Guna.UI2.WinForms;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Helpers;

public static class UiHelper
{
    public static void StyleForm(Form form)
    {
        form.BackColor = ThemeColors.Background;
        form.ForeColor = ThemeColors.Text;
        form.Font = new Font("Segoe UI", 9F);
    }

    public static Guna2Panel CreateCard(int width, int height)
    {
        return new Guna2Panel
        {
            Size = new Size(width, height),
            FillColor = ThemeColors.Card,
            BorderRadius = 12,
            BorderColor = ThemeColors.Border,
            BorderThickness = 1,
            ShadowDecoration = { Enabled = true, Depth = 8, Color = Color.FromArgb(30, 0, 0, 0) }
        };
    }

    public static Label CreateTitleLabel(string text, int x, int y, int size = 11)
    {
        return new Label
        {
            Text = text,
            ForeColor = ThemeColors.TextMuted,
            Font = new Font("Segoe UI Semibold", size),
            AutoSize = true,
            Location = new Point(x, y),
            BackColor = Color.Transparent
        };
    }

    public static Label CreateValueLabel(string text, int x, int y, Color? color = null, int size = 22)
    {
        return new Label
        {
            Text = text,
            ForeColor = color ?? ThemeColors.Text,
            Font = new Font("Segoe UI", size, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(x, y),
            BackColor = Color.Transparent
        };
    }

    public static Guna2Button CreateButton(string text, IconChar icon, Point location, Size? size = null)
    {
        var btn = new Guna2Button
        {
            Text = $"  {text}",
            Location = location,
            Size = size ?? new Size(160, 40),
            FillColor = ThemeColors.Primary,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI Semibold", 9F),
            BorderRadius = 8,
            ImageAlign = HorizontalAlignment.Left,
            TextAlign = HorizontalAlignment.Center,
            Cursor = Cursors.Hand
        };
        btn.Image = icon.ToBitmap(Color.FromArgb(15, 23, 42), 18);
        return btn;
    }

    public static Guna2Button CreateSidebarButton(string text, IconChar icon, Point location)
    {
        var btn = new Guna2Button
        {
            Text = $"  {text}",
            Location = location,
            Size = new Size(220, 45),
            FillColor = Color.Transparent,
            ForeColor = ThemeColors.TextMuted,
            Font = new Font("Segoe UI", 10F),
            BorderRadius = 8,
            TextAlign = HorizontalAlignment.Left,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };
        btn.Image = icon.ToBitmap(ThemeColors.TextMuted, 20);
        btn.MouseEnter += (_, _) =>
        {
            btn.FillColor = Color.FromArgb(30, 41, 59);
            btn.ForeColor = ThemeColors.Primary;
        };
        btn.MouseLeave += (_, _) =>
        {
            if (!btn.Tag?.Equals("active") ?? true)
            {
                btn.FillColor = Color.Transparent;
                btn.ForeColor = ThemeColors.TextMuted;
            }
        };
        return btn;
    }

    public static void SetActiveSidebarButton(Guna2Button active, IEnumerable<Guna2Button> all)
    {
        foreach (var btn in all)
        {
            btn.Tag = btn == active ? "active" : null;
            btn.FillColor = btn == active ? Color.FromArgb(30, 41, 59) : Color.Transparent;
            btn.ForeColor = btn == active ? ThemeColors.Primary : ThemeColors.TextMuted;
        }
    }

    public static void StyleDataGridView(DataGridView dgv)
    {
        dgv.BackgroundColor = ThemeColors.Card;
        dgv.BorderStyle = BorderStyle.None;
        dgv.EnableHeadersVisualStyles = false;
        dgv.GridColor = ThemeColors.Border;
        dgv.DefaultCellStyle.BackColor = ThemeColors.Card;
        dgv.DefaultCellStyle.ForeColor = ThemeColors.Text;
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(56, 189, 248, 80);
        dgv.DefaultCellStyle.SelectionForeColor = ThemeColors.Text;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = ThemeColors.Primary;
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        dgv.ColumnHeadersHeight = 40;
        dgv.RowTemplate.Height = 35;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(25, 35, 55);
        dgv.ReadOnly = true;
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
    }

    public static Color SeverityColor(string severity) => severity switch
    {
        "Critical" => ThemeColors.Danger,
        "High" => Color.FromArgb(249, 115, 22),
        "Medium" => ThemeColors.Warning,
        _ => ThemeColors.Success
    };
}

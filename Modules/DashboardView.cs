using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using FontAwesome.Sharp;
using Guna.UI2.WinForms;
using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Services;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Views.Modules;

public class DashboardView : UserControl
{
    private readonly DashboardController _controller;
    private readonly FlowLayoutPanel _cardsPanel = new();
    private readonly DataGridView _feedGrid = new();
    private readonly CartesianChart _lineChart = new();
    private readonly PieChart _pieChart = new();
    private readonly CartesianChart _barChart = new();
    private readonly CartesianChart _geoChart = new();

    public DashboardView(DashboardController controller)
    {
        _controller = controller;
        InitializeComponent();
        _ = LoadDataAsync();
    }

    private void InitializeComponent()
    {
        BackColor = ThemeColors.Background;
        Dock = DockStyle.Fill;
        AutoScroll = true;

        _cardsPanel.FlowDirection = FlowDirection.LeftToRight;
        _cardsPanel.WrapContents = true;
        _cardsPanel.AutoSize = true;
        _cardsPanel.Dock = DockStyle.Top;
        _cardsPanel.Padding = new Padding(0, 0, 0, 20);
        _cardsPanel.BackColor = Color.Transparent;

        var chartsPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 640,
            ColumnCount = 3,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        chartsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        chartsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        chartsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        chartsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        chartsPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        StyleChart(_lineChart, "Event Trend (24h)");
        StyleChart(_pieChart, "Severity Distribution");
        StyleChart(_barChart, "Top Event Types");
        StyleChart(_geoChart, "GeoIP Distribution");

        chartsPanel.Controls.Add(WrapChart(_lineChart, "Event Trend (24h)"), 0, 0);
        chartsPanel.Controls.Add(WrapChart(_pieChart, "Severity Distribution"), 1, 0);
        chartsPanel.Controls.Add(WrapChart(_barChart, "Top Event Types"), 2, 0);
        var geoPanel = WrapChart(_geoChart, "GeoIP by Country");
        chartsPanel.Controls.Add(geoPanel, 0, 1);
        chartsPanel.SetColumnSpan(geoPanel, 3);

        var feedPanel = UiHelper.CreateCard(1100, 280);
        feedPanel.Dock = DockStyle.Top;
        feedPanel.Margin = new Padding(0, 20, 0, 0);
        feedPanel.Controls.Add(UiHelper.CreateTitleLabel("Live Event Feed", 15, 12, 12));

        _feedGrid.Location = new Point(10, 45);
        _feedGrid.Size = new Size(1050, 220);
        _feedGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        UiHelper.StyleDataGridView(_feedGrid);
        _feedGrid.Columns.AddRange(
            new DataGridViewTextBoxColumn { Name = "Time", HeaderText = "Time", FillWeight = 15 },
            new DataGridViewTextBoxColumn { Name = "EventId", HeaderText = "Event ID", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Type", HeaderText = "Type", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Severity", HeaderText = "Severity", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "SourceIP", HeaderText = "Source IP", FillWeight = 12 },
            new DataGridViewTextBoxColumn { Name = "Mitre", HeaderText = "MITRE", FillWeight = 10 },
            new DataGridViewTextBoxColumn { Name = "Message", HeaderText = "Message", FillWeight = 29 });

        feedPanel.Controls.Add(_feedGrid);

        Controls.Add(feedPanel);
        Controls.Add(chartsPanel);
        Controls.Add(_cardsPanel);
    }

    private static void StyleChart(Control chart, string _)
    {
        chart.Dock = DockStyle.Fill;
        if (chart is CartesianChart cc)
        {
            cc.AnimationsSpeed = TimeSpan.FromMilliseconds(500);
            cc.TooltipTextPaint = new SolidColorPaint(SKColors.White);
        }
        if (chart is PieChart pc)
        {
            pc.AnimationsSpeed = TimeSpan.FromMilliseconds(500);
            pc.TooltipTextPaint = new SolidColorPaint(SKColors.White);
        }
    }

    private static Panel WrapChart(Control chart, string title)
    {
        var panel = UiHelper.CreateCard(350, 300);
        panel.Dock = DockStyle.Fill;
        panel.Margin = new Padding(5);
        panel.Controls.Add(UiHelper.CreateTitleLabel(title, 12, 10, 10));
        chart.Location = new Point(5, 35);
        chart.Size = new Size(340, 250);
        panel.Controls.Add(chart);
        return panel;
    }

    public async void RefreshData() => await LoadDataAsync();

    private async Task LoadDataAsync()
    {
        try
        {
            var metrics = await _controller.GetMetricsAsync();
            BuildMetricCards(metrics);

            var severity = await _controller.GetSeverityDistributionAsync();
            _pieChart.Series = severity.Select(kv => new PieSeries<int>
            {
                Values = new[] { kv.Value },
                Name = kv.Key,
                Fill = new SolidColorPaint(GetSkColor(UiHelper.SeverityColor(kv.Key)))
            }).Cast<ISeries>().ToArray();

            var eventTypes = await _controller.GetEventTypeDistributionAsync();
            _barChart.Series = new ISeries[]
            {
                new ColumnSeries<int>
                {
                    Values = eventTypes.Values.ToArray(),
                    Name = "Events",
                    Fill = new SolidColorPaint(SKColor.Parse("#38BDF8"))
                }
            };
            _barChart.XAxes = new[]
            {
                new Axis { Labels = eventTypes.Keys.ToArray(), LabelsRotation = 30, TextSize = 10 }
            };

            var trend = await _controller.GetHourlyTrendAsync();
            _lineChart.Series = new ISeries[]
            {
                new LineSeries<int>
                {
                    Values = trend.Select(t => t.Count).ToArray(),
                    Fill = new SolidColorPaint(SKColor.Parse("#38BDF8").WithAlpha(50)),
                    Stroke = new SolidColorPaint(SKColor.Parse("#38BDF8"), 2),
                    GeometrySize = 6,
                    Name = "Events"
                }
            };
            _lineChart.XAxes = new[]
            {
                new Axis
                {
                    Labels = trend.Select(t => t.Hour.ToString("HH:mm")).ToArray(),
                    TextSize = 10
                }
            };

            var geo = await _controller.GetGeoDistributionAsync();
            _geoChart.Series = new ISeries[]
            {
                new RowSeries<int>
                {
                    Values = geo.Values.ToArray(),
                    Name = "Events by Country",
                    Fill = new SolidColorPaint(SKColor.Parse("#22C55E"))
                }
            };
            _geoChart.YAxes = new[]
            {
                new Axis { Labels = geo.Keys.ToArray(), TextSize = 11 }
            };

            var feed = await _controller.GetLiveFeedAsync(15);
            _feedGrid.Rows.Clear();
            foreach (var log in feed)
            {
                var idx = _feedGrid.Rows.Add(
                    log.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                    log.EventId, log.EventType, log.Severity, log.SourceIP,
                    log.MitreTechnique ?? "-", log.Message);
                _feedGrid.Rows[idx].Cells["Severity"].Style.ForeColor = UiHelper.SeverityColor(log.Severity);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
        }
    }

    private void BuildMetricCards(DashboardMetrics m)
    {
        _cardsPanel.Controls.Clear();
        var cards = new (string Title, string Value, IconChar Icon, Color Color)[]
        {
            ("Total Logs", m.TotalLogs.ToString("N0"), IconChar.Database, ThemeColors.Primary),
            ("Critical Alerts", m.CriticalAlerts.ToString(), IconChar.SkullCrossbones, ThemeColors.Danger),
            ("High Alerts", m.HighAlerts.ToString(), IconChar.Exclamation, Color.FromArgb(249, 115, 22)),
            ("Open Incidents", m.OpenIncidents.ToString(), IconChar.FolderOpen, ThemeColors.Warning),
            ("Active Users", m.ActiveUsers.ToString(), IconChar.UserCheck, ThemeColors.Success),
            ("Threat Intel Hits", m.ThreatIntelHits.ToString(), IconChar.Biohazard, ThemeColors.Danger),
            ("Today's Events", m.TodaysEvents.ToString(), IconChar.CalendarDay, ThemeColors.Primary),
            ("System Health", $"{m.SystemHealth}%", IconChar.Heartbeat, ThemeColors.Success)
        };

        foreach (var (title, value, icon, color) in cards)
        {
            var card = UiHelper.CreateCard(260, 100);
            card.Margin = new Padding(0, 0, 15, 15);

            var ic = new IconPictureBox
            {
                IconChar = icon, IconColor = color, IconSize = 28,
                Location = new Point(15, 15), BackColor = Color.Transparent
            };
            card.Controls.Add(ic);
            card.Controls.Add(UiHelper.CreateTitleLabel(title, 55, 18));
            card.Controls.Add(UiHelper.CreateValueLabel(value, 55, 42, color, 18));
            _cardsPanel.Controls.Add(card);
        }
    }

    private static SKColor GetSkColor(Color c) => new(c.R, c.G, c.B);
}

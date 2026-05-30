using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Charts;

public static class ChartFactory
{
    public static ISeries[] CreatePieSeries(Dictionary<string, int> data) =>
        data.Select(kv => new PieSeries<int>
        {
            Values = new[] { kv.Value },
            Name = kv.Key,
            Fill = new SolidColorPaint(ToSkColor(UiHelper.SeverityColor(kv.Key)))
        }).Cast<ISeries>().ToArray();

    public static ISeries[] CreateBarSeries(int[] values, string name = "Count") =>
    [
        new ColumnSeries<int>
        {
            Values = values,
            Name = name,
            Fill = new SolidColorPaint(SKColor.Parse("#38BDF8"))
        }
    ];

    public static ISeries[] CreateLineSeries(int[] values, string name = "Events") =>
    [
        new LineSeries<int>
        {
            Values = values,
            Name = name,
            Fill = new SolidColorPaint(SKColor.Parse("#38BDF8").WithAlpha(50)),
            Stroke = new SolidColorPaint(SKColor.Parse("#38BDF8"), 2),
            GeometrySize = 6
        }
    ];

    public static Axis[] CreateCategoryAxis(string[] labels, int rotation = 0) =>
    [
        new Axis { Labels = labels, LabelsRotation = rotation, TextSize = 10 }
    ];

    private static SKColor ToSkColor(Color c) => new(c.R, c.G, c.B);
}

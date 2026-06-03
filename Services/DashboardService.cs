using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CyberWatchSIEM.Services;

public class DashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly LogRepository _logs;
    private readonly AlertRepository _alerts;
    private readonly IncidentRepository _incidents;
    private readonly ThreatIntelRepository _threatIntel;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
        _logs = new LogRepository(context);
        _alerts = new AlertRepository(context);
        _incidents = new IncidentRepository(context);
        _threatIntel = new ThreatIntelRepository(context);
    }

    public async Task<DashboardMetrics> GetMetricsAsync()
    {
        var today = DateTime.UtcNow.Date;
        return new DashboardMetrics
        {
            TotalLogs = await _logs.CountAsync(),
            CriticalAlerts = await _alerts.CountBySeverityAsync("Critical"),
            HighAlerts = await _alerts.CountBySeverityAsync("High"),
            OpenIncidents = await _incidents.CountOpenAsync(),
            ActiveUsers = await _context.Users.CountAsync(u => u.IsActive),
            ThreatIntelHits = await _threatIntel.TotalCountAsync(),
            TodaysEvents = await _logs.GetTodayCountAsync(),
            SystemHealth = CalculateSystemHealth()
        };
    }

    public async Task<Dictionary<string, int>> GetSeverityDistributionAsync()
    {
        var logs = await _logs.GetAllAsync();
        return logs.GroupBy(l => l.Severity)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetEventTypeDistributionAsync()
    {
        var logs = await _logs.GetAllAsync();
        return logs.GroupBy(l => l.EventType)
            .OrderByDescending(g => g.Count())
            .Take(8)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<(DateTime Hour, int Count)>> GetHourlyTrendAsync(int hours = 24)
    {
        var since = DateTime.UtcNow.AddHours(-hours);
        var logs = await _logs.FindAsync(l => l.Timestamp >= since);
        return logs
            .GroupBy(l => new DateTime(l.Timestamp.Year, l.Timestamp.Month, l.Timestamp.Day, l.Timestamp.Hour, 0, 0))
            .OrderBy(g => g.Key)
            .Select(g => (g.Key, g.Count()))
            .ToList();
    }

    public async Task<Dictionary<string, int>> GetGeoDistributionAsync()
    {
        var logs = await _logs.GetRecentAsync(500);
        return logs
            .Where(l => !string.IsNullOrEmpty(l.Country))
            .GroupBy(l => l.Country!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<List<Alert>> GetRecentAlertsAsync(int count = 10) =>
        await _alerts.GetRecentAsync(count);

    public async Task<List<LogEntry>> GetLiveFeedAsync(int count = 20) =>
        await _logs.GetRecentAsync(count);

    private static int CalculateSystemHealth()
    {
        var random = Random.Shared.Next(85, 100);
        return random;
    }
}

public class DashboardMetrics
{
    public int TotalLogs { get; set; }
    public int CriticalAlerts { get; set; }
    public int HighAlerts { get; set; }
    public int OpenIncidents { get; set; }
    public int ActiveUsers { get; set; }
    public int ThreatIntelHits { get; set; }
    public int TodaysEvents { get; set; }
    public int SystemHealth { get; set; }
}

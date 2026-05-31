using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberWatchSIEM.Repositories;

public class LogRepository : Repository<LogEntry>
{
    public LogRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<LogEntry>> SearchAsync(
        string? search, string? severity, string? eventType,
        string? username, string? sourceIp,
        DateTime? from, DateTime? to, int limit = 500)
    {
        var query = DbSet.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(l =>
                l.Message.ToLower().Contains(s) ||
                l.EventId.ToLower().Contains(s) ||
                l.SourceIP.Contains(s) ||
                l.Username.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(l => l.Severity == severity);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(l => l.EventType == eventType);

        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(l => l.Username.Contains(username));

        if (!string.IsNullOrWhiteSpace(sourceIp))
            query = query.Where(l => l.SourceIP.Contains(sourceIp));

        if (from.HasValue)
            query = query.Where(l => l.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.Timestamp <= to.Value);

        return await query.OrderByDescending(l => l.Timestamp).Take(limit).ToListAsync();
    }

    public async Task<List<LogEntry>> GetRecentAsync(int count) =>
        await DbSet.AsNoTracking().OrderByDescending(l => l.Timestamp).Take(count).ToListAsync();

    public async Task<int> GetTodayCountAsync()
    {
        var today = DateTime.UtcNow.Date;
        return await DbSet.CountAsync(l => l.Timestamp >= today);
    }

    public async Task AddRangeAsync(IEnumerable<LogEntry> logs)
    {
        await DbSet.AddRangeAsync(logs);
        await Context.SaveChangesAsync();
    }
}

using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberWatchSIEM.Repositories;

public class AlertRepository : Repository<Alert>
{
    public AlertRepository(ApplicationDbContext context) : base(context) { }

    public async Task<List<Alert>> GetRecentAsync(int count) =>
        await DbSet.AsNoTracking().OrderByDescending(a => a.Time).Take(count).ToListAsync();

    public async Task<int> CountBySeverityAsync(string severity) =>
        await DbSet.CountAsync(a => a.Severity == severity);

    public async Task<List<Alert>> SearchAsync(string? search, string? severity, string? status) =>
        await DbSet.AsNoTracking()
            .Where(a =>
                (string.IsNullOrEmpty(search) || a.Description.Contains(search) || a.AlertCode.Contains(search)) &&
                (string.IsNullOrEmpty(severity) || a.Severity == severity) &&
                (string.IsNullOrEmpty(status) || a.Status == status))
            .OrderByDescending(a => a.Time)
            .ToListAsync();
}

public class IncidentRepository : Repository<Incident>
{
    public IncidentRepository(ApplicationDbContext context) : base(context) { }

    public async Task<int> CountOpenAsync() =>
        await DbSet.CountAsync(i => i.Status == "Open" || i.Status == "Investigating");

    public async Task<List<Incident>> SearchAsync(string? search, string? status, string? severity) =>
        await DbSet.AsNoTracking()
            .Where(i =>
                (string.IsNullOrEmpty(search) || i.AlertName.Contains(search) || i.IncidentCode.Contains(search)) &&
                (string.IsNullOrEmpty(status) || i.Status == status) &&
                (string.IsNullOrEmpty(severity) || i.Severity == severity))
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
}

public class UserRepository : Repository<User>
{
    public UserRepository(ApplicationDbContext context) : base(context) { }

    public async Task<User?> GetByUsernameAsync(string username) =>
        await DbSet.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
}

public class ThreatIntelRepository
{
    private readonly ApplicationDbContext _context;

    public ThreatIntelRepository(ApplicationDbContext context) => _context = context;

    public async Task<List<MaliciousIP>> GetAllIPsAsync() =>
        await _context.MaliciousIPs.AsNoTracking().ToListAsync();

    public async Task<List<SuspiciousDomain>> GetAllDomainsAsync() =>
        await _context.SuspiciousDomains.AsNoTracking().ToListAsync();

    public async Task<List<MalwareSignature>> GetAllSignaturesAsync() =>
        await _context.MalwareSignatures.AsNoTracking().ToListAsync();

    public async Task AddIPAsync(MaliciousIP entity)
    {
        _context.MaliciousIPs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddDomainAsync(SuspiciousDomain entity)
    {
        _context.SuspiciousDomains.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task AddSignatureAsync(MalwareSignature entity)
    {
        _context.MalwareSignatures.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateIPAsync(MaliciousIP entity)
    {
        _context.MaliciousIPs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateDomainAsync(SuspiciousDomain entity)
    {
        _context.SuspiciousDomains.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateSignatureAsync(MalwareSignature entity)
    {
        _context.MalwareSignatures.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteIPAsync(MaliciousIP entity)
    {
        _context.MaliciousIPs.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteDomainAsync(SuspiciousDomain entity)
    {
        _context.SuspiciousDomains.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteSignatureAsync(MalwareSignature entity)
    {
        _context.MalwareSignatures.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task<int> TotalCountAsync() =>
        await _context.MaliciousIPs.CountAsync() +
        await _context.SuspiciousDomains.CountAsync() +
        await _context.MalwareSignatures.CountAsync();
}

public class AuditRepository : Repository<AuditLog>
{
    public AuditRepository(ApplicationDbContext context) : base(context) { }

    public async Task LogAsync(string username, string action, string entityType, string details)
    {
        await AddAsync(new AuditLog
        {
            Username = username,
            Action = action,
            EntityType = entityType,
            Details = details,
            Timestamp = DateTime.UtcNow
        });
    }
}

public class SettingsRepository : Repository<AppSetting>
{
    public SettingsRepository(ApplicationDbContext context) : base(context) { }

    public async Task<string?> GetValueAsync(string key)
    {
        var setting = await DbSet.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value;
    }

    public async Task SetValueAsync(string key, string value, string category = "General")
    {
        var setting = await DbSet.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            await AddAsync(new AppSetting { Key = key, Value = value, Category = category });
        }
        else
        {
            setting.Value = value;
            await UpdateAsync(setting);
        }
    }
}

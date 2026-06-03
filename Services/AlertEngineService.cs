using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Services;

public class AlertEngineService
{
    private readonly ApplicationDbContext _context;
    private readonly AlertRepository _alerts;
    private readonly SettingsRepository _settings;
    private readonly ThreatIntelRepository _threatIntel;

    private static readonly string[] MalwareKeywords = ["malware", "trojan", "virus", "ransomware", "worm"];

    public event Action<Alert>? AlertGenerated;

    public AlertEngineService(ApplicationDbContext context)
    {
        _context = context;
        _alerts = new AlertRepository(context);
        _settings = new SettingsRepository(context);
        _threatIntel = new ThreatIntelRepository(context);
    }

    public async Task ProcessLogAsync(LogEntry log)
    {
        await CheckMalwareAsync(log);
        await CheckBruteForceAsync(log);
        await CheckPortScanAsync(log);
        await CheckSuspiciousLoginAsync(log);
        await CheckThreatIntelAsync(log);
    }

    private async Task<int> GetThresholdAsync(string key, int defaultValue)
    {
        var val = await _settings.GetValueAsync(key);
        return int.TryParse(val, out var n) ? n : defaultValue;
    }

    private async Task CheckMalwareAsync(LogEntry log)
    {
        var messageLower = log.Message.ToLower();
        if (!MalwareKeywords.Any(k => messageLower.Contains(k) || log.EventType == "Malware")) return;

        await CreateAlertAsync(
            AppConstants.AlertTypes.Malware,
            AppConstants.Severity.Critical,
            $"Malware detected: {log.Message}",
            log.SourceIP,
            log.LogId);
    }

    private async Task CheckBruteForceAsync(LogEntry log)
    {
        if (log.EventType != "Failed Login") return;

        var threshold = await GetThresholdAsync("BruteForceThreshold", 5);
        var since = DateTime.UtcNow.AddMinutes(-15);
        var count = _context.Logs.Count(l =>
            l.SourceIP == log.SourceIP &&
            l.EventType == "Failed Login" &&
            l.Timestamp >= since);

        if (count > threshold)
        {
            await CreateAlertAsync(
                AppConstants.AlertTypes.BruteForce,
                AppConstants.Severity.High,
                $"Brute force attack detected from {log.SourceIP} ({count} failed attempts)",
                log.SourceIP,
                log.LogId);
        }
    }

    private async Task CheckPortScanAsync(LogEntry log)
    {
        if (log.EventType != "Port Scan") return;

        var threshold = await GetThresholdAsync("PortScanThreshold", 20);
        var since = DateTime.UtcNow.AddMinutes(-10);
        var count = _context.Logs.Count(l =>
            l.SourceIP == log.SourceIP &&
            l.EventType == "Port Scan" &&
            l.Timestamp >= since);

        if (count > threshold)
        {
            await CreateAlertAsync(
                AppConstants.AlertTypes.PortScan,
                AppConstants.Severity.High,
                $"Port scan detected from {log.SourceIP} ({count} port probes)",
                log.SourceIP,
                log.LogId);
        }
    }

    private async Task CheckSuspiciousLoginAsync(LogEntry log)
    {
        if (log.EventType != "Login" || string.IsNullOrEmpty(log.Username)) return;

        var recent = _context.Logs
            .Where(l => l.Username == log.Username && l.EventType == "Login" && l.LogId != log.LogId)
            .OrderByDescending(l => l.Timestamp)
            .FirstOrDefault();

        if (recent != null && recent.Country != null && log.Country != null &&
            recent.Country != log.Country && recent.SourceIP != log.SourceIP)
        {
            await CreateAlertAsync(
                AppConstants.AlertTypes.SuspiciousLogin,
                AppConstants.Severity.Medium,
                $"Suspicious login for {log.Username} from {log.Country} ({log.SourceIP})",
                log.SourceIP,
                log.LogId);
        }
    }

    private async Task CheckThreatIntelAsync(LogEntry log)
    {
        var maliciousIps = await _threatIntel.GetAllIPsAsync();
        if (maliciousIps.Any(m => m.IPAddress == log.SourceIP || m.IPAddress == log.DestinationIP))
        {
            await CreateAlertAsync(
                AppConstants.AlertTypes.ThreatIntel,
                AppConstants.Severity.Critical,
                $"Threat intel match: IP {log.SourceIP} found in malicious database",
                log.SourceIP,
                log.LogId);
        }

        var domains = await _threatIntel.GetAllDomainsAsync();
        foreach (var domain in domains)
        {
            if (log.Message.Contains(domain.Domain, StringComparison.OrdinalIgnoreCase))
            {
                await CreateAlertAsync(
                    AppConstants.AlertTypes.ThreatIntel,
                    AppConstants.Severity.High,
                    $"Threat intel match: domain {domain.Domain} in log message",
                    log.SourceIP,
                    log.LogId);
            }
        }
    }

    private async Task CreateAlertAsync(string type, string severity, string description, string? sourceIp, int? logId)
    {
        var recentDuplicate = _context.Alerts.Any(a =>
            a.AlertType == type &&
            a.SourceIP == sourceIp &&
            a.Time >= DateTime.UtcNow.AddMinutes(-5) &&
            a.Description == description);

        if (recentDuplicate) return;

        var alert = new Alert
        {
            AlertCode = $"ALT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(100, 999)}",
            Severity = severity,
            Time = DateTime.UtcNow,
            Description = description,
            Status = "New",
            SourceIP = sourceIp,
            AlertType = type,
            RelatedLogId = logId
        };

        await _alerts.AddAsync(alert);
        AlertGenerated?.Invoke(alert);
    }
}

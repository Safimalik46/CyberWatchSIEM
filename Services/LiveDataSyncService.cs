using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;
using Microsoft.EntityFrameworkCore;

namespace CyberWatchSIEM.Services;

/// <summary>
/// Syncs real public threat feeds into the database and generates live SIEM events from them.
/// </summary>
public class LiveDataSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly LiveThreatFeedClient _feedClient;
    private readonly LogRepository _logs;
    private readonly ThreatIntelRepository _threatIntel;
    private readonly SettingsRepository _settings;
    private readonly AuditService _audit;

    private LiveFeedSnapshot? _snapshot;
    private int _eventIndex;

    public LiveFeedSnapshot? LastSnapshot => _snapshot;
    public bool HasLiveData => _snapshot?.IsOnline == true;

    public LiveDataSyncService(ApplicationDbContext context, LiveThreatFeedClient feedClient)
    {
        _context = context;
        _feedClient = feedClient;
        _logs = new LogRepository(context);
        _threatIntel = new ThreatIntelRepository(context);
        _settings = new SettingsRepository(context);
        _audit = new AuditService(context);
    }

    public async Task<LiveFeedSnapshot> SyncAllAsync(CancellationToken ct = default)
    {
        var snapshot = new LiveFeedSnapshot();
        try
        {
            var feodo = await _feedClient.FetchFeodoTrackerIpsAsync(ct);
            var spamhaus = await _feedClient.FetchSpamhausDropAsync(ct);
            var urlhaus = await _feedClient.FetchUrlhausDomainsAsync(ct);
            var cisa = await _feedClient.FetchCisaKnownExploitedAsync(ct);

            snapshot.MaliciousIps = feodo
                .Concat(spamhaus)
                .GroupBy(i => i.IP)
                .Select(g => g.First())
                .Take(200)
                .ToList();

            snapshot.Domains = urlhaus.Take(100).ToList();
            snapshot.SecurityEvents = cisa;
            snapshot.IsOnline = snapshot.MaliciousIps.Count > 0 || snapshot.Domains.Count > 0;
            snapshot.FetchedAt = DateTime.UtcNow;

            await PersistThreatIntelAsync(snapshot);
            await CreateInitialLiveLogsAsync(snapshot, ct);

            await _settings.SetValueAsync("LiveDataLastSync", snapshot.FetchedAt.ToString("O"), "LiveData");
            await _settings.SetValueAsync("LiveDataStatus", snapshot.IsOnline ? "Online" : "Offline", "LiveData");
            await _settings.SetValueAsync("LiveDataIpCount", snapshot.MaliciousIps.Count.ToString(), "LiveData");
            await _settings.SetValueAsync("LiveDataDomainCount", snapshot.Domains.Count.ToString(), "LiveData");

            _snapshot = snapshot;
        }
        catch (Exception ex)
        {
            snapshot.IsOnline = false;
            snapshot.ErrorMessage = ex.Message;
            _snapshot = snapshot;
            await _settings.SetValueAsync("LiveDataStatus", $"Error: {ex.Message}", "LiveData");
        }

        return snapshot;
    }

    public async Task<LogEntry?> GenerateNextLiveEventAsync(CancellationToken ct = default)
    {
        if (_snapshot == null || !_snapshot.IsOnline)
            return null;

        var events = BuildEventQueue();
        if (events.Count == 0) return null;

        var template = events[_eventIndex % events.Count];
        _eventIndex++;

        var log = new LogEntry
        {
            EventId = $"LIVE-{DateTime.UtcNow:yyyyMMddHHmmss}-{_eventIndex}",
            Timestamp = DateTime.UtcNow,
            SourceIP = template.SourceIP ?? "0.0.0.0",
            DestinationIP = "10.0.0.1",
            Username = "system",
            EventType = template.EventType,
            Severity = template.Severity,
            Message = template.Message,
            MitreTechnique = template.MitreTechnique,
            ThreatScore = template.Severity switch
            {
                "Critical" => 95,
                "High" => 75,
                "Medium" => 50,
                _ => 25
            }
        };

        if (!string.IsNullOrEmpty(template.SourceIP) && template.SourceIP != "0.0.0.0")
        {
            var geo = await _feedClient.LookupGeoIpAsync(template.SourceIP, ct);
            if (geo != null)
            {
                log.Country = geo.CountryCode;
                log.Message += $" [Geo: {geo.Country}, ISP: {geo.Isp}]";
            }
        }

        return log;
    }

    private List<LiveSecurityEvent> BuildEventQueue()
    {
        if (_snapshot == null) return [];

        var queue = new List<LiveSecurityEvent>(_snapshot.SecurityEvents);

        foreach (var ip in _snapshot.MaliciousIps.Take(50))
        {
            queue.Add(new LiveSecurityEvent
            {
                EventId = $"TI-{ip.IP}",
                EventType = "Malware",
                Severity = ip.Severity,
                SourceIP = ip.IP,
                Message = $"Live feed match: connection to known malicious IP {ip.IP} ({ip.Source}) — {ip.Description}",
                MitreTechnique = "T1071"
            });
        }

        foreach (var domain in _snapshot.Domains.Take(30))
        {
            queue.Add(new LiveSecurityEvent
            {
                EventId = $"TI-{domain.Domain}",
                EventType = "Malware",
                Severity = domain.Severity,
                Message = $"Live feed: DNS/HTTP request to malicious domain {domain.Domain} ({domain.Source})",
                MitreTechnique = "T1204"
            });
        }

        return queue;
    }

    private async Task PersistThreatIntelAsync(LiveFeedSnapshot snapshot)
    {
        var existingIps = (await _threatIntel.GetAllIPsAsync()).Select(i => i.IPAddress).ToHashSet();
        foreach (var ip in snapshot.MaliciousIps)
        {
            if (existingIps.Contains(ip.IP)) continue;
            await _threatIntel.AddIPAsync(new MaliciousIP
            {
                IPAddress = ip.IP,
                ThreatType = ip.Source,
                Description = ip.Description,
                Severity = ip.Severity
            });
            existingIps.Add(ip.IP);
        }

        var existingDomains = (await _threatIntel.GetAllDomainsAsync()).Select(d => d.Domain).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var domain in snapshot.Domains)
        {
            if (existingDomains.Contains(domain.Domain)) continue;
            await _threatIntel.AddDomainAsync(new SuspiciousDomain
            {
                Domain = domain.Domain,
                Category = domain.Source,
                Description = domain.Description,
                Severity = domain.Severity
            });
            existingDomains.Add(domain.Domain);
        }
    }

    private async Task CreateInitialLiveLogsAsync(LiveFeedSnapshot snapshot, CancellationToken ct)
    {
        var count = 0;
        foreach (var ip in snapshot.MaliciousIps.Take(15))
        {
            var geo = await _feedClient.LookupGeoIpAsync(ip.IP, ct);
            await _logs.AddAsync(new LogEntry
            {
                EventId = $"LIVE-INIT-{ip.IP.Replace(".", "")}",
                Timestamp = DateTime.UtcNow.AddSeconds(-count),
                SourceIP = ip.IP,
                DestinationIP = "10.0.0.1",
                Username = "system",
                EventType = "Network Activity",
                Severity = ip.Severity,
                Message = $"[LIVE FEED] Threat intel ingest: {ip.Source} listed {ip.IP} — {ip.Description}" +
                          (geo != null ? $" Origin: {geo.Country} ({geo.Isp})" : ""),
                Country = geo?.CountryCode,
                MitreTechnique = "T1071",
                ThreatScore = 90
            });
            count++;
        }

        foreach (var cve in snapshot.SecurityEvents.Take(10))
        {
            await _logs.AddAsync(new LogEntry
            {
                EventId = cve.EventId,
                Timestamp = DateTime.UtcNow.AddSeconds(-count),
                SourceIP = "0.0.0.0",
                DestinationIP = "10.0.0.1",
                Username = "system",
                EventType = cve.EventType,
                Severity = cve.Severity,
                Message = $"[LIVE FEED] {cve.Message}",
                MitreTechnique = cve.MitreTechnique,
                ThreatScore = 95
            });
            count++;
        }
    }
}

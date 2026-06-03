using System.Globalization;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace CyberWatchSIEM.Services;

/// <summary>
/// Fetches real threat intelligence from public feeds (abuse.ch, Spamhaus, CISA).
/// No API key required for these sources.
/// </summary>
public class LiveThreatFeedClient
{
    private static readonly HttpClient Http = CreateClient();
    private readonly Dictionary<string, GeoIpResult> _geoCache = new();

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(45) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("CyberWatchSIEM/1.0 (Educational SIEM Project)");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        return client;
    }

    public async Task<List<LiveMaliciousIp>> FetchFeodoTrackerIpsAsync(CancellationToken ct = default)
    {
        const string url = "https://feodotracker.abuse.ch/downloads/ipblocklist.csv";
        var text = await Http.GetStringAsync(url, ct);
        var results = new List<LiveMaliciousIp>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith('#')) continue;
            var parts = ParseCsvLine(line.Trim());
            var ip = parts.Count >= 2 ? parts[1].Trim('"') : parts[0].Trim('"');
            if (IsPublicIp(ip))
                results.Add(new LiveMaliciousIp(ip, "Feodo Tracker", "Emotet/TrickBot C2 botnet IP (abuse.ch)", "Critical"));
        }

        return results;
    }

    public async Task<List<LiveMaliciousIp>> FetchSpamhausDropAsync(CancellationToken ct = default)
    {
        const string url = "https://www.spamhaus.org/drop/drop.txt";
        var text = await Http.GetStringAsync(url, ct);
        var results = new List<LiveMaliciousIp>();

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith(';')) continue;
            var ip = line.Split(';')[0].Trim();
            if (IsPublicIp(ip))
                results.Add(new LiveMaliciousIp(ip, "Spamhaus DROP", "Hijacked/spam network block (Spamhaus DROP)", "High"));
        }

        return results;
    }

    public async Task<List<LiveSuspiciousDomain>> FetchUrlhausDomainsAsync(CancellationToken ct = default)
    {
        const string url = "https://urlhaus.abuse.ch/downloads/csv_recent/";
        var text = await Http.GetStringAsync(url, ct);
        var results = new List<LiveSuspiciousDomain>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1))
        {
            if (line.StartsWith('#')) continue;
            var parts = ParseCsvLine(line);
            if (parts.Count < 3) continue;

            var threatUrl = parts[2].Trim('"');
            if (string.IsNullOrWhiteSpace(threatUrl)) continue;

            if (!Uri.TryCreate(threatUrl, UriKind.Absolute, out var uri)) continue;
            var domain = uri.Host;
            if (seen.Add(domain))
            {
                var threat = parts.Count > 6 ? parts[6].Trim('"') : "malware_download";
                results.Add(new LiveSuspiciousDomain(domain, "URLhaus", $"Active malware distribution URL ({threat})", "Critical"));
            }
        }

        return results.Take(100).ToList();
    }

    public async Task<List<LiveSecurityEvent>> FetchCisaKnownExploitedAsync(CancellationToken ct = default)
    {
        const string url = "https://www.cisa.gov/sites/default/files/feeds/known_exploited_vulnerabilities.json";
        var json = await Http.GetStringAsync(url, ct);
        var root = JObject.Parse(json);
        var vulns = root["vulnerabilities"] as JArray ?? [];

        return vulns.Take(25).Select(v => new LiveSecurityEvent
        {
            EventId = v["cveID"]?.ToString() ?? "CVE-UNKNOWN",
            EventType = "Vulnerability",
            Severity = "Critical",
            Message = $"CISA KEV: {v["vulnerabilityName"]} — {v["shortDescription"]}",
            SourceLabel = v["vendorProject"]?.ToString() ?? "Multiple",
            MitreTechnique = "T1190"
        }).ToList();
    }

    public async Task<GeoIpResult?> LookupGeoIpAsync(string ip, CancellationToken ct = default)
    {
        if (!IsPublicIp(ip)) return null;
        if (_geoCache.TryGetValue(ip, out var cached)) return cached;

        try
        {
            var url = $"http://ip-api.com/json/{ip}?fields=status,country,countryCode,query,isp,org";
            var json = JObject.Parse(await Http.GetStringAsync(url, ct));
            if (json["status"]?.ToString() != "success") return null;

            var result = new GeoIpResult(
                json["query"]!.ToString(),
                json["countryCode"]!.ToString(),
                json["country"]!.ToString(),
                json["isp"]?.ToString() ?? "",
                json["org"]?.ToString() ?? "");

            _geoCache[ip] = result;
            await Task.Delay(1500, ct); // ip-api.com free tier: ~45 req/min
            return result;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPublicIp(string ip) =>
        Regex.IsMatch(ip, @"^\d{1,3}(\.\d{1,3}){3}$") &&
        !ip.StartsWith("10.") && !ip.StartsWith("192.168.") && !ip.StartsWith("127.");

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = "";
        foreach (var c in line)
        {
            if (c == '"') { inQuotes = !inQuotes; continue; }
            if (c == ',' && !inQuotes) { result.Add(current); current = ""; continue; }
            current += c;
        }
        result.Add(current);
        return result;
    }
}

public record LiveMaliciousIp(string IP, string Source, string Description, string Severity);
public record LiveSuspiciousDomain(string Domain, string Source, string Description, string Severity);
public record GeoIpResult(string IP, string CountryCode, string Country, string Isp, string Org);

public class LiveSecurityEvent
{
    public string EventId { get; set; } = "";
    public string EventType { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Message { get; set; } = "";
    public string SourceLabel { get; set; } = "";
    public string? SourceIP { get; set; }
    public string? MitreTechnique { get; set; }
}

public class LiveFeedSnapshot
{
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    public List<LiveMaliciousIp> MaliciousIps { get; set; } = [];
    public List<LiveSuspiciousDomain> Domains { get; set; } = [];
    public List<LiveSecurityEvent> SecurityEvents { get; set; } = [];
    public bool IsOnline { get; set; }
    public string? ErrorMessage { get; set; }
}

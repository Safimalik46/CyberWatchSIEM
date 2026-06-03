using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using CyberWatchSIEM.Database;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;
using Newtonsoft.Json;

namespace CyberWatchSIEM.Services;

public class LogService
{
    private readonly LogRepository _logs;
    private readonly AuditService _audit;
    private readonly AlertEngineService _alertEngine;

    public LogService(ApplicationDbContext context, AlertEngineService alertEngine)
    {
        _logs = new LogRepository(context);
        _audit = new AuditService(context);
        _alertEngine = alertEngine;
    }

    public Task<List<LogEntry>> SearchAsync(string? search, string? severity, string? eventType,
        string? username, string? sourceIp, DateTime? from, DateTime? to) =>
        _logs.SearchAsync(search, severity, eventType, username, sourceIp, from, to);

    public Task<List<LogEntry>> GetRecentAsync(int count) => _logs.GetRecentAsync(count);
    public Task<int> GetTotalCountAsync() => _logs.CountAsync();
    public Task<int> GetTodayCountAsync() => _logs.GetTodayCountAsync();
    public Task<LogEntry?> GetByIdAsync(int id) => _logs.GetByIdAsync(id);

    public async Task AddLogAsync(LogEntry log)
    {
        log.ThreatScore = CalculateThreatScore(log);
        await _logs.AddAsync(log);
        await _audit.LogCreateAsync("Log", $"Added log {log.EventId}");
        await _alertEngine.ProcessLogAsync(log);
    }

    public async Task UpdateLogAsync(LogEntry log)
    {
        log.ThreatScore = CalculateThreatScore(log);
        await _logs.UpdateAsync(log);
        await _audit.LogUpdateAsync("Log", $"Updated log {log.EventId}");
    }

    public async Task DeleteLogAsync(int id)
    {
        var log = await _logs.GetByIdAsync(id);
        if (log == null) return;
        await _logs.DeleteAsync(log);
        await _audit.LogDeleteAsync("Log", $"Deleted log {log.EventId}");
    }

    public async Task<int> ImportFromFileAsync(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var imported = ext switch
        {
            ".csv" => await ImportCsvAsync(filePath),
            ".json" => await ImportJsonAsync(filePath),
            ".txt" => await ImportTxtAsync(filePath),
            _ => throw new InvalidOperationException("Unsupported file format.")
        };

        foreach (var log in imported)
            await _alertEngine.ProcessLogAsync(log);

        await _audit.LogCreateAsync("Log", $"Imported {imported.Count} logs from {Path.GetFileName(filePath)}");
        return imported.Count;
    }

    private async Task<List<LogEntry>> ImportCsvAsync(string filePath)
    {
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        var records = csv.GetRecords<LogImportDto>().ToList();
        var logs = records.Select(MapToLog).ToList();
        await _logs.AddRangeAsync(logs);
        return logs;
    }

    private async Task<List<LogEntry>> ImportJsonAsync(string filePath)
    {
        var json = await File.ReadAllTextAsync(filePath);
        var records = JsonConvert.DeserializeObject<List<LogImportDto>>(json) ?? [];
        var logs = records.Select(MapToLog).ToList();
        await _logs.AddRangeAsync(logs);
        return logs;
    }

    private async Task<List<LogEntry>> ImportTxtAsync(string filePath)
    {
        var lines = await File.ReadAllLinesAsync(filePath);
        var logs = new List<LogEntry>();
        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            logs.Add(new LogEntry
            {
                EventId = $"EVT-{Guid.NewGuid():N}"[..12],
                Timestamp = DateTime.UtcNow,
                SourceIP = "0.0.0.0",
                DestinationIP = "0.0.0.0",
                Username = "import",
                EventType = "Network Activity",
                Severity = "Low",
                Message = line.Trim()
            });
        }
        await _logs.AddRangeAsync(logs);
        return logs;
    }

    public async Task ExportToCsvAsync(string filePath, List<LogEntry> logs)
    {
        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(logs.Select(l => new
        {
            l.EventId, l.Timestamp, l.SourceIP, l.DestinationIP,
            l.Username, l.EventType, l.Severity, l.Message
        }));
    }

    private static LogEntry MapToLog(LogImportDto dto) => new()
    {
        EventId = string.IsNullOrEmpty(dto.EventId) ? $"EVT-{Guid.NewGuid():N}"[..12] : dto.EventId,
        Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp,
        SourceIP = dto.SourceIP ?? "0.0.0.0",
        DestinationIP = dto.DestinationIP ?? "0.0.0.0",
        Username = dto.Username ?? "unknown",
        EventType = dto.EventType ?? "Network Activity",
        Severity = dto.Severity ?? "Low",
        Message = dto.Message ?? string.Empty,
        ThreatScore = CalculateThreatScore(dto.Severity ?? "Low", dto.Message ?? "")
    };

    public static int CalculateThreatScore(LogEntry log) =>
        CalculateThreatScore(log.Severity, log.Message);

    private static int CalculateThreatScore(string severity, string message)
    {
        var score = severity switch
        {
            "Critical" => 85,
            "High" => 65,
            "Medium" => 40,
            _ => 15
        };

        var keywords = new[] { "malware", "ransomware", "exploit", "attack" };
        if (keywords.Any(k => message.Contains(k, StringComparison.OrdinalIgnoreCase)))
            score = Math.Min(100, score + 20);

        return score;
    }

    private class LogImportDto
    {
        public string? EventId { get; set; }
        public DateTime Timestamp { get; set; }
        public string? SourceIP { get; set; }
        public string? DestinationIP { get; set; }
        public string? Username { get; set; }
        public string? EventType { get; set; }
        public string? Severity { get; set; }
        public string? Message { get; set; }
    }
}

public class LogSimulatorService : IDisposable
{
    private readonly LiveDataSyncService? _liveSync;
    private readonly LogService _logService;
    private System.Windows.Forms.Timer? _timer;
    private readonly Random _rng = new();
    private bool _useLiveData;

    public event Action<LogEntry>? LogGenerated;

    public LogSimulatorService(ApplicationDbContext context, LogService logService, LiveDataSyncService? liveSync = null)
    {
        _liveSync = liveSync;
        _logService = logService;
    }

    public void SetLiveMode(bool enabled) => _useLiveData = enabled && _liveSync?.HasLiveData == true;

    public void Start(int intervalMs = 3000)
    {
        _timer?.Stop();
        _timer = new System.Windows.Forms.Timer { Interval = intervalMs };
        _timer.Tick += async (_, _) => await GenerateLogAsync();
        _timer.Start();
    }

    public void Stop() => _timer?.Stop();

    private async Task GenerateLogAsync()
    {
        LogEntry log;

        if (_useLiveData && _liveSync != null)
        {
            var liveLog = await _liveSync.GenerateNextLiveEventAsync();
            if (liveLog != null)
            {
                await _logService.AddLogAsync(liveLog);
                LogGenerated?.Invoke(liveLog);
                return;
            }
        }

        log = GenerateFallbackLog();
        await _logService.AddLogAsync(log);
        LogGenerated?.Invoke(log);
    }

    private LogEntry GenerateFallbackLog()
    {
        var eventTypes = new[] { "Login", "Failed Login", "Port Scan", "Malware", "Network Activity" };
        var eventType = eventTypes[_rng.Next(eventTypes.Length)];
        return new LogEntry
        {
            EventId = $"SIM-{DateTime.UtcNow:HHmmss}-{_rng.Next(1000, 9999)}",
            Timestamp = DateTime.UtcNow,
            SourceIP = $"{_rng.Next(1, 223)}.{_rng.Next(0, 255)}.{_rng.Next(0, 255)}.{_rng.Next(1, 254)}",
            DestinationIP = $"10.0.{_rng.Next(0, 255)}.{_rng.Next(1, 254)}",
            Username = $"user{_rng.Next(1, 30)}",
            EventType = eventType,
            Severity = eventType switch { "Malware" => "Critical", "Port Scan" => "High", "Failed Login" => "Medium", _ => "Low" },
            Message = $"[OFFLINE MODE] Simulated {eventType} — connect to internet for live threat feeds",
            Country = new[] { "US", "RU", "CN", "DE" }[_rng.Next(4)],
            MitreTechnique = eventType switch { "Failed Login" => "T1110", "Port Scan" => "T1046", "Malware" => "T1204", _ => "T1071" }
        };
    }

    public void Dispose() => _timer?.Dispose();
}

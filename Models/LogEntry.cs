namespace CyberWatchSIEM.Models;

public class LogEntry
{
    public int LogId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string SourceIP { get; set; } = string.Empty;
    public string DestinationIP { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low";
    public string Message { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? MitreTechnique { get; set; }
    public int ThreatScore { get; set; }
}

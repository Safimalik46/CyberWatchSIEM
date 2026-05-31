namespace CyberWatchSIEM.Models;

public class Alert
{
    public int AlertId { get; set; }
    public string AlertCode { get; set; } = string.Empty;
    public string Severity { get; set; } = "Low";
    public DateTime Time { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string? SourceIP { get; set; }
    public string AlertType { get; set; } = string.Empty;
    public int? RelatedLogId { get; set; }
}

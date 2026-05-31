namespace CyberWatchSIEM.Models;

public class MaliciousIP
{
    public int Id { get; set; }
    public string IPAddress { get; set; } = string.Empty;
    public string ThreatType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "High";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class SuspiciousDomain
{
    public int Id { get; set; }
    public string Domain { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Medium";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

public class MalwareSignature
{
    public int Id { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string MalwareFamily { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Critical";
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

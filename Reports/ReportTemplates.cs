namespace CyberWatchSIEM.Reports;

/// <summary>
/// Report metadata and template constants for the report module.
/// </summary>
public static class ReportTemplates
{
    public const string SecurityReportTitle = "CyberWatch SIEM - Security Report";
    public const string IncidentReportTitle = "Incident Report";
    public const string AlertReportTitle = "Alert Report";
    public const string ThreatIntelReportTitle = "Threat Intelligence Report";

    public static string GenerateFileName(string reportType, string extension) =>
        $"{reportType.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
}

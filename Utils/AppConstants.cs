namespace CyberWatchSIEM.Utils;

public static class AppConstants
{
    public const string AppName = "CyberWatch SIEM";
    public const string DefaultAdminUsername = "admin";
    public const string DefaultAdminPassword = "admin123";

    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Analyst = "Analyst";
    }

    public static class Severity
    {
        public const string Critical = "Critical";
        public const string High = "High";
        public const string Medium = "Medium";
        public const string Low = "Low";
    }

    public static class IncidentStatus
    {
        public const string Open = "Open";
        public const string Investigating = "Investigating";
        public const string Resolved = "Resolved";
        public const string Closed = "Closed";
    }

    public static class AlertTypes
    {
        public const string BruteForce = "Brute Force";
        public const string PortScan = "Port Scan";
        public const string Malware = "Malware Detection";
        public const string SuspiciousLogin = "Suspicious Login";
        public const string ThreatIntel = "Threat Intel Match";
    }
}

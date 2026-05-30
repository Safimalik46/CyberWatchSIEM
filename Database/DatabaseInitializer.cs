using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Utils;
using Microsoft.EntityFrameworkCore;

namespace CyberWatchSIEM.Database;

public static class DatabaseInitializer
{
    private static readonly Random Rng = new();

    private static readonly string[] EventTypes =
        ["Login", "Failed Login", "Port Scan", "Malware", "Network Activity", "Firewall", "DNS Query"];

    private static readonly string[] Severities = ["Critical", "High", "Medium", "Low"];
    private static readonly string[] Countries = ["US", "RU", "CN", "DE", "UK", "BR", "IN", "FR", "JP", "KR"];
    private static readonly string[] MitreTechniques = ["T1110", "T1046", "T1204", "T1071", "T1059", "T1566"];

    public static void Initialize()
    {
        using var context = new ApplicationDbContext();
        context.Database.EnsureCreated();
        SeedIfEmpty(context);
    }

    private static void SeedIfEmpty(ApplicationDbContext context)
    {
        if (context.Users.Any()) return;

        SeedUsers(context);
        SeedSettings(context);
        SeedThreatIntel(context);
        SeedLogs(context);
        SeedAlerts(context);
        SeedIncidents(context);
        context.SaveChanges();
    }

    private static void SeedUsers(ApplicationDbContext context)
    {
        var users = new List<User>
        {
            new()
            {
                Username = AppConstants.DefaultAdminUsername,
                PasswordHash = SecurityHelper.HashPassword(AppConstants.DefaultAdminPassword),
                FullName = "System Administrator",
                Email = "admin@cyberwatch.local",
                Role = AppConstants.Roles.Admin
            }
        };

        var firstNames = new[] { "Alex", "Jordan", "Sam", "Taylor", "Morgan", "Casey", "Riley", "Quinn", "Avery", "Blake" };
        for (var i = 1; i < 10; i++)
        {
            users.Add(new User
            {
                Username = $"analyst{i}",
                PasswordHash = SecurityHelper.HashPassword("analyst123"),
                FullName = $"{firstNames[i - 1]} Analyst",
                Email = $"analyst{i}@cyberwatch.local",
                Role = AppConstants.Roles.Analyst
            });
        }

        context.Users.AddRange(users);
    }

    private static void SeedSettings(ApplicationDbContext context)
    {
        var settings = new[]
        {
            new AppSetting { Key = "Theme", Value = "Dark", Category = "UI" },
            new AppSetting { Key = "BruteForceThreshold", Value = "5", Category = "Alerts" },
            new AppSetting { Key = "PortScanThreshold", Value = "20", Category = "Alerts" },
            new AppSetting { Key = "NotificationsEnabled", Value = "true", Category = "Notifications" },
            new AppSetting { Key = "DashboardRefreshSeconds", Value = "10", Category = "Dashboard" },
            new AppSetting { Key = "SimulatorEnabled", Value = "true", Category = "Simulator" },
            new AppSetting { Key = "UseLiveDataFeed", Value = "true", Category = "LiveData" },
            new AppSetting { Key = "LiveDataLastSync", Value = "", Category = "LiveData" },
            new AppSetting { Key = "LiveDataStatus", Value = "Not synced", Category = "LiveData" }
        };
        context.Settings.AddRange(settings);
    }

    private static void SeedThreatIntel(ApplicationDbContext context)
    {
        // Minimal offline fallback — real feeds sync on startup via LiveDataSyncService
        context.MalwareSignatures.Add(new MalwareSignature
        {
            Signature = "Emotet",
            MalwareFamily = "Emotet",
            Description = "Placeholder — replaced by live URLhaus/Feodo feeds when online",
            Severity = "Critical"
        });
    }

    private static void SeedLogs(ApplicationDbContext context)
    {
        // Small demo baseline only — live threat feeds add real events on startup
        var logs = new List<LogEntry>();
        for (var i = 0; i < 50; i++)
        {
            var eventType = EventTypes[Rng.Next(EventTypes.Length)];
            var severity = eventType switch
            {
                "Malware" => "Critical",
                "Failed Login" => Rng.Next(3) == 0 ? "High" : "Medium",
                "Port Scan" => "High",
                _ => Severities[Rng.Next(Severities.Length)]
            };

            logs.Add(new LogEntry
            {
                EventId = $"EVT-{10000 + i}",
                Timestamp = DateTime.UtcNow.AddMinutes(-Rng.Next(0, 10080)),
                SourceIP = $"10.{Rng.Next(0, 255)}.{Rng.Next(0, 255)}.{Rng.Next(1, 254)}",
                DestinationIP = $"172.16.{Rng.Next(0, 255)}.{Rng.Next(1, 254)}",
                Username = $"user{Rng.Next(1, 50)}",
                EventType = eventType,
                Severity = severity,
                Message = "[SAMPLE DATA] Demo security event — live feeds active when online",
                Country = Countries[Rng.Next(Countries.Length)],
                MitreTechnique = MitreTechniques[Rng.Next(MitreTechniques.Length)],
                ThreatScore = severity switch
                {
                    "Critical" => Rng.Next(80, 100),
                    "High" => Rng.Next(60, 79),
                    "Medium" => Rng.Next(30, 59),
                    _ => Rng.Next(0, 29)
                }
            });
        }

        context.Logs.AddRange(logs);
    }

    private static void SeedAlerts(ApplicationDbContext context)
    {
        var types = new[] { "Brute Force", "Port Scan", "Malware Detection", "Suspicious Login", "Threat Intel Match" };
        for (var i = 0; i < 20; i++)
        {
            context.Alerts.Add(new Alert
            {
                AlertCode = $"ALT-{2000 + i}",
                Severity = Severities[Rng.Next(Severities.Length)],
                Time = DateTime.UtcNow.AddHours(-Rng.Next(0, 720)),
                Description = $"[SAMPLE] {types[Rng.Next(types.Length)]} alert placeholder",
                Status = Rng.Next(3) == 0 ? "Acknowledged" : "New",
                SourceIP = $"203.0.{Rng.Next(0, 255)}.{Rng.Next(1, 254)}",
                AlertType = types[Rng.Next(types.Length)]
            });
        }
    }

    private static void SeedIncidents(ApplicationDbContext context)
    {
        var statuses = new[] { "Open", "Investigating", "Resolved", "Closed" };
        for (var i = 0; i < 10; i++)
        {
            context.Incidents.Add(new Incident
            {
                IncidentCode = $"INC-{3000 + i}",
                AlertName = $"[SAMPLE] Security Incident #{i + 1}",
                Severity = Severities[Rng.Next(Severities.Length)],
                Status = statuses[Rng.Next(statuses.Length)],
                AssignedTo = $"analyst{Rng.Next(1, 10)}",
                Description = "Investigation required for anomalous network activity",
                Resolution = statuses[i % statuses.Length] is "Resolved" or "Closed" ? "Mitigated and monitored" : ""
            });
        }
    }

    private static string GenerateMessage(string eventType) => eventType switch
    {
        "Login" => "Successful user authentication",
        "Failed Login" => "Authentication failed - invalid credentials",
        "Port Scan" => "Multiple port connection attempts detected",
        "Malware" => "Malware signature detected in network traffic",
        "Network Activity" => "Unusual outbound network connection",
        _ => "Security event recorded"
    };
}

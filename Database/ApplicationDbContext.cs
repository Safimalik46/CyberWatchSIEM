using CyberWatchSIEM.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberWatchSIEM.Database;

public class ApplicationDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<LogEntry> Logs => Set<LogEntry>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<Incident> Incidents => Set<Incident>();
    public DbSet<MaliciousIP> MaliciousIPs => Set<MaliciousIP>();
    public DbSet<SuspiciousDomain> SuspiciousDomains => Set<SuspiciousDomain>();
    public DbSet<MalwareSignature> MalwareSignatures => Set<MalwareSignature>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AppSetting> Settings => Set<AppSetting>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(
                @"Server=(localdb)\MSSQLLocalDB;Database=CyberWatchSIEM;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(x => x.UserId);
            e.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<LogEntry>(e =>
        {
            e.HasKey(x => x.LogId);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.SourceIP);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(x => x.AlertId);
            e.HasIndex(x => x.Time);
        });

        modelBuilder.Entity<Incident>(e =>
        {
            e.HasKey(x => x.IncidentId);
        });

        modelBuilder.Entity<MaliciousIP>().HasKey(x => x.Id);
        modelBuilder.Entity<SuspiciousDomain>().HasKey(x => x.Id);
        modelBuilder.Entity<MalwareSignature>().HasKey(x => x.Id);

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(x => x.AuditId);
            e.HasIndex(x => x.Timestamp);
        });

        modelBuilder.Entity<AppSetting>(e =>
        {
            e.HasKey(x => x.SettingId);
            e.HasIndex(x => x.Key).IsUnique();
        });
    }
}

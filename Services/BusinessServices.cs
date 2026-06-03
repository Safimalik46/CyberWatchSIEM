using CyberWatchSIEM.Database;
using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Repositories;
using CyberWatchSIEM.Utils;
using Microsoft.EntityFrameworkCore;

namespace CyberWatchSIEM.Services;

public class UserManagementService
{
    private readonly UserRepository _users;
    private readonly AuditService _audit;

    public UserManagementService(ApplicationDbContext context)
    {
        _users = new UserRepository(context);
        _audit = new AuditService(context);
    }

    public Task<List<User>> GetAllAsync() => _users.GetAllAsync();

    public async Task<(bool Success, string Message)> CreateUserAsync(User user, string password)
    {
        if (!SecurityHelper.IsValidUsername(user.Username) || !SecurityHelper.IsValidPassword(password))
            return (false, "Invalid username or password.");

        var existing = await _users.GetByUsernameAsync(user.Username);
        if (existing != null) return (false, "Username already exists.");

        user.PasswordHash = SecurityHelper.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;
        await _users.AddAsync(user);
        await _audit.LogCreateAsync("User", $"Created user {user.Username}");
        return (true, "User created successfully.");
    }

    public async Task UpdateUserAsync(User user)
    {
        await _users.UpdateAsync(user);
        await _audit.LogUpdateAsync("User", $"Updated user {user.Username}");
    }

    public async Task DeleteUserAsync(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null || user.Username == AppConstants.DefaultAdminUsername) return;
        user.IsActive = false;
        await _users.UpdateAsync(user);
        await _audit.LogDeleteAsync("User", $"Deactivated user {user.Username}");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(int userId, string newPassword)
    {
        if (!SecurityHelper.IsValidPassword(newPassword))
            return (false, "Password must be at least 6 characters.");

        var user = await _users.GetByIdAsync(userId);
        if (user == null) return (false, "User not found.");

        user.PasswordHash = SecurityHelper.HashPassword(newPassword);
        await _users.UpdateAsync(user);
        await _audit.LogUpdateAsync("User", $"Reset password for {user.Username}");
        return (true, "Password reset successfully.");
    }
}

public class IncidentService
{
    private readonly IncidentRepository _incidents;
    private readonly AuditService _audit;

    public IncidentService(ApplicationDbContext context)
    {
        _incidents = new IncidentRepository(context);
        _audit = new AuditService(context);
    }

    public Task<List<Incident>> GetAllAsync() => _incidents.GetAllAsync();

    public Task<List<Incident>> SearchAsync(string? search, string? status, string? severity) =>
        _incidents.SearchAsync(search, status, severity);

    public async Task CreateAsync(Incident incident)
    {
        incident.IncidentCode = $"INC-{DateTime.UtcNow:yyyyMMddHHmmss}";
        incident.CreatedAt = DateTime.UtcNow;
        await _incidents.AddAsync(incident);
        await _audit.LogCreateAsync("Incident", incident.AlertName);
    }

    public async Task UpdateAsync(Incident incident)
    {
        incident.UpdatedAt = DateTime.UtcNow;
        await _incidents.UpdateAsync(incident);
        await _audit.LogUpdateAsync("Incident", incident.AlertName);
    }

    public async Task DeleteAsync(int id)
    {
        var incident = await _incidents.GetByIdAsync(id);
        if (incident == null) return;
        await _incidents.DeleteAsync(incident);
        await _audit.LogDeleteAsync("Incident", incident.AlertName);
    }
}

public class AlertService
{
    private readonly AlertRepository _alerts;
    private readonly AuditService _audit;

    public AlertService(ApplicationDbContext context)
    {
        _alerts = new AlertRepository(context);
        _audit = new AuditService(context);
    }

    public Task<List<Alert>> GetAllAsync() => _alerts.GetAllAsync();
    public Task<List<Alert>> SearchAsync(string? search, string? severity, string? status) =>
        _alerts.SearchAsync(search, severity, status);

    public async Task UpdateStatusAsync(int alertId, string status)
    {
        var alert = await _alerts.GetByIdAsync(alertId);
        if (alert == null) return;
        alert.Status = status;
        await _alerts.UpdateAsync(alert);
        await _audit.LogUpdateAsync("Alert", alert.AlertCode);
    }
}

public class SettingsService
{
    private readonly SettingsRepository _settings;
    private readonly AuditService _audit;

    public SettingsService(ApplicationDbContext context)
    {
        _settings = new SettingsRepository(context);
        _audit = new AuditService(context);
    }

    public Task<List<AppSetting>> GetAllAsync() => _settings.GetAllAsync();
    public Task<string?> GetAsync(string key) => _settings.GetValueAsync(key);

    public async Task SetAsync(string key, string value, string category = "General")
    {
        await _settings.SetValueAsync(key, value, category);
        await _audit.LogUpdateAsync("Settings", key);
    }

    public async Task BackupToJsonAsync(string filePath)
    {
        await using var context = new ApplicationDbContext();
        var backup = new
        {
            ExportedAt = DateTime.UtcNow,
            Users = await context.Users.AsNoTracking().ToListAsync(),
            Logs = await context.Logs.AsNoTracking().OrderByDescending(l => l.LogId).Take(500).ToListAsync(),
            Alerts = await context.Alerts.AsNoTracking().ToListAsync(),
            Incidents = await context.Incidents.AsNoTracking().ToListAsync()
        };
        await File.WriteAllTextAsync(filePath, Newtonsoft.Json.JsonConvert.SerializeObject(backup, Newtonsoft.Json.Formatting.Indented));
        await _audit.LogCreateAsync("Settings", $"Backup exported to {Path.GetFileName(filePath)}");
    }

    public async Task ApplyThemeAsync()
    {
        var theme = await GetAsync("Theme") ?? "Dark";
        if (theme.Equals("Light", StringComparison.OrdinalIgnoreCase))
            Utils.ThemeColors.ApplyLightTheme();
        else
            Utils.ThemeColors.ApplyDarkTheme();
    }
}

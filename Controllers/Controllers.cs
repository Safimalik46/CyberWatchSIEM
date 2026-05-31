using CyberWatchSIEM.Helpers;
using CyberWatchSIEM.Models;
using CyberWatchSIEM.Services;
using CyberWatchSIEM.Utils;

namespace CyberWatchSIEM.Controllers;

public class AuthController
{
    private readonly AuthService _auth;

    public AuthController(AuthService auth) => _auth =  auth;

    public async Task<(bool Success, string Message)> LoginAsync(string username, string password, bool rememberMe)
    {
        var result = await _auth.LoginAsync(username, password);
        if (result.Success && result.User != null)
            SessionManager.StartSession(result.User, rememberMe);
        return (result.Success, result.Message);
    }

    public Task LogoutAsync()  => _auth.LogoutAsync();
}

public class DashboardController
{
    private readonly DashboardService _dashboard;

    public DashboardController(DashboardService dashboard) => _dashboard = dashboard;

    public Task<DashboardMetrics> GetMetricsAsync() => _dashboard.GetMetricsAsync();
    public Task<Dictionary<string, int>> GetSeverityDistributionAsync() => _dashboard.GetSeverityDistributionAsync();
    public Task<Dictionary<string, int>> GetEventTypeDistributionAsync() => _dashboard.GetEventTypeDistributionAsync();
    public Task<List<(DateTime Hour, int Count)>> GetHourlyTrendAsync() => _dashboard.GetHourlyTrendAsync();
    public Task<Dictionary<string, int>> GetGeoDistributionAsync() => _dashboard.GetGeoDistributionAsync();
    public Task<List<Alert>> GetRecentAlertsAsync() => _dashboard.GetRecentAlertsAsync();
    public Task<List<LogEntry>> GetLiveFeedAsync(int count = 20) => _dashboard.GetLiveFeedAsync(count);
}

public class LogController
{
    private readonly LogService _logs;

    public LogController(LogService logs) => _logs = logs;

    public Task<List<LogEntry>> SearchAsync(string? search, string? severity, string? eventType,
        string? username, string? sourceIp, DateTime? from, DateTime? to) =>
        _logs.SearchAsync(search, severity, eventType, username, sourceIp, from, to);

    public Task AddAsync(LogEntry log) => _logs.AddLogAsync(log);
    public Task UpdateAsync(LogEntry log) => _logs.UpdateLogAsync(log);
    public Task DeleteAsync(int id) => _logs.DeleteLogAsync(id);
    public Task<int> ImportAsync(string path) => _logs.ImportFromFileAsync(path);
    public Task ExportCsvAsync(string path, List<LogEntry> logs) => _logs.ExportToCsvAsync(path, logs);
    public Task<List<LogEntry>> GetRecentAsync(int count) => _logs.GetRecentAsync(count);
    public Task<LogEntry?> GetByIdAsync(int id) => _logs.GetByIdAsync(id);
}

public class AlertController
{
    private readonly AlertService _alerts;

    public AlertController(AlertService alerts) => _alerts = alerts;

    public Task<List<Alert>> GetAllAsync() => _alerts.GetAllAsync();
    public Task<List<Alert>> SearchAsync(string? s, string? sev, string? st) => _alerts.SearchAsync(s, sev, st);
    public Task UpdateStatusAsync(int id, string status) => _alerts.UpdateStatusAsync(id, status);
}

public class IncidentController
{
    private readonly IncidentService _incidents;

    public IncidentController(IncidentService incidents) => _incidents = incidents;

    public Task<List<Incident>> GetAllAsync() => _incidents.GetAllAsync();
    public Task<List<Incident>> SearchAsync(string? s, string? st, string? sev) => _incidents.SearchAsync(s, st, sev);
    public Task CreateAsync(Incident incident) => _incidents.CreateAsync(incident);
    public Task UpdateAsync(Incident incident) => _incidents.UpdateAsync(incident);
    public Task DeleteAsync(int id) => _incidents.DeleteAsync(id);
}

public class UserController
{
    private readonly UserManagementService _users;

    public UserController(UserManagementService users) => _users = users;

    public Task<List<User>> GetAllAsync() => _users.GetAllAsync();
    public Task<(bool, string)> CreateAsync(User user, string password) => _users.CreateUserAsync(user, password);
    public Task UpdateAsync(User user) => _users.UpdateUserAsync(user);
    public Task DeleteAsync(int id) => _users.DeleteUserAsync(id);
    public Task<(bool, string)> ResetPasswordAsync(int id, string pwd) => _users.ResetPasswordAsync(id, pwd);
}

public class SettingsController
{
    private readonly SettingsService _settings;

    public SettingsController(SettingsService settings) => _settings = settings;

    public Task<List<AppSetting>> GetAllAsync() => _settings.GetAllAsync();
    public Task SetAsync(string key, string value, string category) => _settings.SetAsync(key, value, category);
    public Task<string?> GetAsync(string key) => _settings.GetAsync(key);
    public Task BackupAsync(string path) => _settings.BackupToJsonAsync(path);
    public Task ApplyThemeAsync() => _settings.ApplyThemeAsync();
}

public class ReportController
{
    private readonly ReportService _reports;

    public ReportController(ReportService reports) => _reports = reports;

    public Task ExportSecurityPdfAsync(string path) => _reports.ExportSecurityReportPdfAsync(path);
    public Task ExportIncidentExcelAsync(string path) => _reports.ExportIncidentReportExcelAsync(path);
    public Task ExportAlertCsvAsync(string path) => _reports.ExportAlertReportCsvAsync(path);
    public Task ExportThreatIntelPdfAsync(string path) => _reports.ExportThreatIntelReportPdfAsync(path);
}

public class SearchController
{
    private readonly GlobalSearchService _search;

    public SearchController(GlobalSearchService search) => _search = search;

    public Task<List<SearchResult>> SearchAsync(string query) => _search.SearchAsync(query);
}

public class SimulatorController
{
    private readonly LogSimulatorService _simulator;

    public SimulatorController(LogSimulatorService simulator) => _simulator = simulator;

    public void EnableLiveMode(bool enabled) => _simulator.SetLiveMode(enabled);
    public void Start() => _simulator.Start(3000);
    public void Stop() => _simulator.Stop();
    public event Action<LogEntry>? LogGenerated
    {
        add => _simulator.LogGenerated += value;
        remove => _simulator.LogGenerated -= value;
    }
}

public class ThreatIntelController
{
    private readonly ThreatIntelService _service;

    public ThreatIntelController(ThreatIntelService service) => _service = service;

    public Task<List<MaliciousIP>> GetIPsAsync() => _service.GetIPsAsync();
    public Task<List<SuspiciousDomain>> GetDomainsAsync() => _service.GetDomainsAsync();
    public Task<List<MalwareSignature>> GetSignaturesAsync() => _service.GetSignaturesAsync();
    public Task AddIPAsync(MaliciousIP entity) => _service.AddIPAsync(entity);
    public Task AddDomainAsync(SuspiciousDomain entity) => _service.AddDomainAsync(entity);
    public Task AddSignatureAsync(MalwareSignature entity) => _service.AddSignatureAsync(entity);
    public Task DeleteIPAsync(int id) => _service.DeleteIPAsync(id);
    public Task DeleteDomainAsync(int id) => _service.DeleteDomainAsync(id);
    public Task DeleteSignatureAsync(int id) => _service.DeleteSignatureAsync(id);
}

public class LiveDataController
{
    private readonly LiveDataSyncService _liveData;
    private readonly SimulatorController? _simulator;

    public LiveDataController(LiveDataSyncService liveData, SimulatorController? simulator = null)
    {
        _liveData = liveData;
        _simulator = simulator;
    }

    public async Task<LiveFeedSnapshot> SyncAsync()
    {
        var snapshot = await _liveData.SyncAllAsync();
        _simulator?.EnableLiveMode(snapshot.IsOnline);
        return snapshot;
    }

    public LiveFeedSnapshot? LastSnapshot => _liveData.LastSnapshot;
    public bool HasLiveData => _liveData.HasLiveData;
}

public class AuditController
{
    private readonly AuditService _audit;

    public AuditController(AuditService audit) => _audit = audit;

    public Task<List<AuditLog>> GetAllAsync() => _audit.GetAllAsync();
}

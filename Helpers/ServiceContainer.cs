using CyberWatchSIEM.Database;
using CyberWatchSIEM.Services;

namespace CyberWatchSIEM.Helpers;

public sealed class ServiceContainer : IDisposable
{
    private static ServiceContainer? _instance;
    public static ServiceContainer Instance => _instance ??= new ServiceContainer();

    public ApplicationDbContext Context { get; } = new();
    public NotificationService Notifications { get; } = new();
    public AlertEngineService AlertEngine { get; }
    public AuthService Auth { get; }
    public LogService Logs { get; }
    public DashboardService Dashboard { get; }
    public UserManagementService Users { get; }
    public IncidentService Incidents { get; }
    public AlertService Alerts { get; }
    public SettingsService Settings { get; }
    public ReportService Reports { get; }
    public AuditService Audit { get; }
    public GlobalSearchService Search { get; }
    public LogSimulatorService Simulator { get; }

    private ServiceContainer()
    {
        AlertEngine = new AlertEngineService(Context);
        Auth = new AuthService(Context);
        Logs = new LogService(Context, AlertEngine);
        Dashboard = new DashboardService(Context);
        Users = new UserManagementService(Context);
        Incidents = new IncidentService(Context);
        Alerts = new AlertService(Context);
        Settings = new SettingsService(Context);
        Reports = new ReportService(Context);
        Audit = new AuditService(Context);
        Search = new GlobalSearchService(Context);
        Simulator = new LogSimulatorService(Context, Logs);

        AlertEngine.AlertGenerated += alert => Notifications.NotifyAlert(alert);
    }

    public void Dispose()
    {
        Simulator.Dispose();
        Context.Dispose();
        _instance = null;
    }
}

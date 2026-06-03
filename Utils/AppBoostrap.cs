using CyberWatchSIEM.Database;
using CyberWatchSIEM.Repositories;
using CyberWatchSIEM.Services;

namespace CyberWatchSIEM.Utils;

public static class AppBootstrap
{
    public static ApplicationDbContext CreateContext() => new();

    public static (AuthService Auth, DashboardService Dashboard, LogService Logs,
        AlertEngineService AlertEngine, AlertService Alerts, IncidentService Incidents,
        UserManagementService Users, ThreatIntelService ThreatIntel,
        SettingsService Settings, ReportService Reports, NotificationService Notifications,
        GlobalSearchService Search, AuditService Audit, LogSimulatorService Simulator,
        LiveDataSyncService LiveData, LiveThreatFeedClient FeedClient)
        InitializeServices()
    {
        var context = CreateContext();
        var feedClient = new LiveThreatFeedClient();
        var liveData = new LiveDataSyncService(context, feedClient);

        var notifications = new NotificationService();
        var alertEngine = new AlertEngineService(context);
        alertEngine.AlertGenerated += notifications.NotifyAlert;

        var logService = new LogService(context, alertEngine);
        var simulator = new LogSimulatorService(context, logService, liveData);

        return (
            new AuthService(context),
            new DashboardService(context),
            logService,
            alertEngine,
            new AlertService(context),
            new IncidentService(context),
            new UserManagementService(context),
            new ThreatIntelService(context),
            new SettingsService(context),
            new ReportService(context),
            notifications,
            new GlobalSearchService(context),
            new AuditService(context),
            simulator,
            liveData,
            feedClient
        );
    }
}

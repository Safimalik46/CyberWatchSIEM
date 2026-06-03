using CyberWatchSIEM.Controllers;
using CyberWatchSIEM.Database;
using CyberWatchSIEM.Utils;
using CyberWatchSIEM.Views;

namespace CyberWatchSIEM;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            DatabaseInitializer.Initialize();

            var services = AppBootstrap.InitializeServices();
            services.Settings.ApplyThemeAsync().GetAwaiter().GetResult();

            var simulatorController = new SimulatorController(services.Simulator);
            var liveController = new LiveDataController(services.LiveData, simulatorController);

            // Sync real threat intelligence from public feeds (requires internet)
            liveController.SyncAsync().GetAwaiter().GetResult();

            var authController = new AuthController(services.Auth);

            using var loginForm = new LoginForm(authController);
            if (loginForm.ShowDialog() != DialogResult.OK)
                return;

            Application.Run(new MainShellForm(
                authController,
                new DashboardController(services.Dashboard),
                new LogController(services.Logs),
                new AlertController(services.Alerts),
                new IncidentController(services.Incidents),
                new UserController(services.Users),
                new SettingsController(services.Settings),
                new ReportController(services.Reports),
                new SearchController(services.Search),
                simulatorController,
                services.Notifications,
                new ThreatIntelController(services.ThreatIntel),
                new AuditController(services.Audit),
                liveController));
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to start CyberWatch SIEM:\n\n{ex.Message}",
                "Startup Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}

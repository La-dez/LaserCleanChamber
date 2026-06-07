using LaserCleanChamber.Configuration;
using LaserCleanChamber.Logging;
using System;
using System.Windows;

namespace LaserCleanChamber
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = SettingsManager.Load();
            AppLogging.Initialize(settings.Logging);
            AppLogging.App.Information(AppLogging.Prefix("APP", "Action=Startup, SessionId={SessionId}"), AppLogging.CurrentSessionId);

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        protected override void OnExit(ExitEventArgs e)
        {
            AppLogging.App.Information(AppLogging.Prefix("APP", "Action=Shutdown, ExitCode={ExitCode}"), e.ApplicationExitCode);

            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            DispatcherUnhandledException -= App_DispatcherUnhandledException;
            AppLogging.CloseAndFlush();

            base.OnExit(e);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                if (e.IsTerminating)
                {
                    AppLogging.App.Fatal(exception, AppLogging.Prefix("APP", "Action=UnhandledException, Source=AppDomain, IsTerminating={IsTerminating}"), e.IsTerminating);
                }
                else
                {
                    AppLogging.App.Error(exception, AppLogging.Prefix("APP", "Action=UnhandledException, Source=AppDomain, IsTerminating={IsTerminating}"), e.IsTerminating);
                }
            }
            else
            {
                if (e.IsTerminating)
                {
                    AppLogging.App.Fatal(AppLogging.Prefix("APP", "Action=UnhandledException, Source=AppDomain, HasException=false, IsTerminating={IsTerminating}"), e.IsTerminating);
                }
                else
                {
                    AppLogging.App.Error(AppLogging.Prefix("APP", "Action=UnhandledException, Source=AppDomain, HasException=false, IsTerminating={IsTerminating}"), e.IsTerminating);
                }
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            AppLogging.App.Fatal(e.Exception, AppLogging.Prefix("APP", "Action=UnhandledException, Source=Dispatcher"));
        }
    }

}

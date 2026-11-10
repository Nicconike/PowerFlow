using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PowerFlow.Core.Configuration;
using PowerFlow.Core.Providers;
using PowerFlow.Core.Services;

namespace PowerFlow.App;

internal static class Program
{
    private const string MutexName = @"Global\PowerFlow_SingleInstance_Mutex_83LY";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(true, MutexName, out var isOnlyInstance);
        if (!isOnlyInstance)
        {
            MessageBox.Show(
                "PowerFlow is already running in the System Tray.",
                "PowerFlow",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<PowerFlowOptions>(hostContext.Configuration.GetSection(PowerFlowOptions.SectionName));

                services.AddSingleton<IPowerPlanManager, WindowsPowerManager>();
                services.AddSingleton<HardwareProviderFactory>();
                services.AddSingleton(sp => sp.GetRequiredService<HardwareProviderFactory>().CreateProvider());
                services.AddSingleton<IProcessDetector, ProcessDetector>();
                services.AddSingleton<IProfileCoordinator, ProfileCoordinator>();

                services.AddSingleton<TrayApplicationContext>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            });

        var host = builder.Build();

        using var serviceScope = host.Services.CreateScope();
        var services = serviceScope.ServiceProvider;

        try
        {
            var trayContext = services.GetRequiredService<TrayApplicationContext>();
            Application.Run(trayContext);
        }
        catch (Exception ex)
        {
            var logger = services.GetService<ILogger<TrayApplicationContext>>();
            logger?.LogCritical(ex, "Fatal error occurred in PowerFlow.App");
            MessageBox.Show($"Fatal Error: {ex.Message}", "PowerFlow Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            host.Dispose();
        }
    }
}

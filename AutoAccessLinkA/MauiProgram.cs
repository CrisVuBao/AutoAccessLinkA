using Microsoft.Extensions.Logging;
using Serilog;

#if WINDOWS
using H.NotifyIcon;
#endif

namespace AutoAccessLinkA
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            // Setup Serilog
            var logPath = Path.Combine(FileSystem.AppDataDirectory, "Logs", "log-.txt");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .WriteTo.Debug()
                .CreateLogger();

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
#if WINDOWS
                .UseNotifyIcon()
#endif
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Use Serilog for standard logging
            builder.Logging.AddSerilog(dispose: true);

            Console.WriteLine(FileSystem.AppDataDirectory);

            return builder.Build();
        }
    }
}

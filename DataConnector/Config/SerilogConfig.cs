using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace DataConnector.Config
{
    public static class SerilogConfig
    {
        public static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Literate // Enables colorized logs
                )
                .CreateLogger();
        }
    }
}
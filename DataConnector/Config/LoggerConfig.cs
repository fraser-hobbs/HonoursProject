using Microsoft.Extensions.Logging;

namespace DataConnector.Config;

public class LoggerConfig
{
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public ILoggerFactory CreateLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel);
            builder.AddConsole(); 
        });
    }
}
using Microsoft.Extensions.Logging;

namespace DataConnector.Config;

public class DataConnectorConfig
{
    public required string KafkaServer { get; set; }
    public required string KafkaTopic { get; set; }
    public required string ClientId { get; set; }
    public LogLevel LogLevel { get; set; } = LogLevel.Information;
}
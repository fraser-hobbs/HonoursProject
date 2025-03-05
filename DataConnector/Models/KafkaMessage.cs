namespace DataConnector.Models;

public class KafkaMessage<T>
{
    public required string Source { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public required T Payload { get; set; }
}
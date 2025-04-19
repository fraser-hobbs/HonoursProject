namespace BuildingDataController.Models;

public class Record
{
    public required string BuildingId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
namespace SolarDataController.Models;

public class Record
{
    public required string ArrayId { get; set; }
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}
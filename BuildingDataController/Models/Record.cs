namespace BuildingDataController.Models;

public class Record
{
    public DateTime TimeStamp { get; set; }
    public double Value { get; set; }
    public required string BuildingId { get; set; }
}
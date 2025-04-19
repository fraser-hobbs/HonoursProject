namespace SolarDataController.Models;

public class Record
{
    public int GspId { get; set; }
    public DateTime Timestamp { get; set; }
    public double GenerationMw { get; set; }
}
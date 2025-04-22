namespace SolarDataController.Interfaces;

public interface IDataFeedService
{
    Task StartAsync(CancellationToken cancellationToken);
}
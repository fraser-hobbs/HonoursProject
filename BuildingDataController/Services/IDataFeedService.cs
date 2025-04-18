namespace BuildingDataController.Services;

public interface IDataFeedService
{
    Task StartAsync(CancellationToken cancellationToken);
}
using SolarDataController.Interfaces;

namespace SolarDataController;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IDataFeedService _dataFeedService;

    public Worker(ILogger<Worker> logger, IDataFeedService dataFeedService)
    {
        _logger = logger;
        _dataFeedService = dataFeedService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker started at (UTC): {time}", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss"));
        _logger.LogInformation("Development Mode: {mode}", true);
        await _dataFeedService.StartAsync(stoppingToken);
    }
}
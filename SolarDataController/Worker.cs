using System.Text.Json;
using DataConnector;
using DataConnector.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolarDataController.Models;
using SolarDataController.Services;

namespace SolarDataController;

public class SolarWorker : BackgroundService
{
    private readonly DataFetcher _dataFetcher;
    private readonly IDataConnector? _dataConnector;
    private readonly ILogger<SolarWorker> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);
    private readonly bool _isDevelopmentMode;

    public SolarWorker(DataFetcher dataFetcher, IDataConnector? dataConnector, ILogger<SolarWorker> logger)
    {
        _dataFetcher = dataFetcher;
        _logger = logger;

        _isDevelopmentMode = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";

        // Only assign DataConnector if not in development mode
        if (!_isDevelopmentMode)
        {
            _dataConnector = dataConnector;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SolarWorker started. Development Mode: {Mode}", _isDevelopmentMode);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Fetching solar data...");
                var records = await _dataFetcher.FetchSolarDataAsync();

                if (records.Any())
                {
                    _logger.LogInformation("Processing {Count} records...", records.Count);

                    foreach (var record in records)
                    {
                        if (_isDevelopmentMode)
                        {
                            // Log the API response instead of sending to Kafka
                            _logger.LogInformation("Development Mode: API Response - {Message}", JsonSerializer.Serialize(record));
                        }
                        else
                        {
                            await _dataConnector!.SendMessageAsync(record);
                        }
                    }

                    _logger.LogInformation("Processing complete.");
                }
                else
                {
                    _logger.LogWarning("No new solar data available.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in SolarWorker.");
            }

            _logger.LogInformation("Waiting for the next interval...");
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
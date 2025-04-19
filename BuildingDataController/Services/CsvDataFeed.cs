using BuildingDataController.Helpers;
using BuildingDataController.Models;
using DataConnector.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuildingDataController.Services;

public class CsvDataFeed : IDataFeedService
{
    private readonly CsvLoader _csvLoader;
    private readonly IDataConnector _dataConnector;
    private readonly ILogger<CsvDataFeed> _logger;

    public CsvDataFeed(CsvLoader csvLoader, IDataConnector dataConnector, ILogger<CsvDataFeed> logger)
    {
        _csvLoader = csvLoader;
        _dataConnector = dataConnector;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var allRecords = _csvLoader.Load().OrderBy(r => r.Timestamp).ToList();

        if (!allRecords.Any())
        {
            _logger.LogWarning("No records loaded from CSV.");
            return;
        }
        
        var debugPreview = Environment.GetEnvironmentVariable("DEBUG_PREVIEW") == "true";

        // DEBUGGING LOGIC
        if (debugPreview)
        {
            int numberOfDebugRecordsToShow = 5;

            var simulatedNow = new DateTime(2025, 4, 24, 0, 13, 0);
            var simulatedDayOfWeek = simulatedNow.DayOfWeek;
            var currentRoundedTime = simulatedNow.Minute < 30 ? new TimeSpan(simulatedNow.Hour, 0, 0) : new TimeSpan(simulatedNow.Hour, 30, 0);

            _logger.LogInformation("Emulation will begin at: {0}", simulatedNow.ToString("dd/MM/yyyy HH:mm"));

            // Step 1: Find the correct starting record matching day of week and time
            var startRecord = allRecords.FirstOrDefault(r =>
                r.Timestamp.DayOfWeek == simulatedDayOfWeek && r.Timestamp.TimeOfDay == TimeSpan.Zero);

            if (startRecord == null)
            {
                _logger.LogWarning("No starting record found for given simulated day. Defaulting to first record.");
                startRecord = allRecords.First();
            }

            var startDate = startRecord.Timestamp.Date;

            var targetTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, currentRoundedTime.Hours, currentRoundedTime.Minutes, 0);

            var debugStartIndex = allRecords.FindIndex(r => r.Timestamp == targetTime);

            if (debugStartIndex == -1)
            {
                _logger.LogWarning("Exact start time not found. Defaulting to first record.");
                debugStartIndex = 0;
            }

            _logger.LogInformation("Next {0} matching records to be sent:", numberOfDebugRecordsToShow);
            for (int i = 0; i < numberOfDebugRecordsToShow; i++)
            {
                var index = debugStartIndex + i;
                if (index >= allRecords.Count)
                    break;

                var record = allRecords[index];
                var emulatedDateTime = simulatedNow.AddMinutes(i * 30);

                _logger.LogInformation("Building: {0} | Emulated Date: {1} | CSV Date: {2} {3:HH:mm} | Value: {4}",
                    record.BuildingId,
                    emulatedDateTime.ToString("dd/MM/yyyy HH:mm"),
                    record.Timestamp.ToString("dd/MM/yyyy"),
                    record.Timestamp,
                    record.Value);
            }
        }

        // PRODUCTION / LIVE LOGIC
        var now = DateTime.Now;
        var roundedNow = now.Minute < 30
            ? new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0)
            : new DateTime(now.Year, now.Month, now.Day, now.Hour, 30, 0);
        var currentDayOfWeek = roundedNow.DayOfWeek;
        var currentTime = roundedNow.TimeOfDay;

        // Find the first date in the CSV with the same DayOfWeek as today
        var csvStartDate = allRecords.First().Timestamp.Date;
        var csvStartDayOfWeek = allRecords.First().Timestamp.DayOfWeek;

        var alignedDate = allRecords
            .Select(r => r.Timestamp.Date)
            .Distinct()
            .FirstOrDefault(d => d.DayOfWeek == currentDayOfWeek);

        if (alignedDate == default)
        {
            _logger.LogWarning("No matching day of week found in CSV.");
            alignedDate = allRecords.First().Timestamp.Date;
        }

        // Combine aligned date with current interval time
        var alignedStartTime = alignedDate.Add(currentTime);

        // Find first record that matches the aligned start time
        var alignedIndex = allRecords.FindIndex(r => r.Timestamp == alignedStartTime);
        if (alignedIndex < 0)
        {
            _logger.LogWarning("No matching start record found. Defaulting to first record.");
            alignedIndex = 0;
        }
        var startIndex = alignedIndex;

        for (int index = startIndex; index < allRecords.Count && !cancellationToken.IsCancellationRequested; index++)
        {
            var record = allRecords[index];

            var emulatedTimestamp = DateTime.SpecifyKind(roundedNow.AddMinutes((index - startIndex) * 30), DateTimeKind.Utc);
            var emulatedRecord = new
            {
                TimeStamp = emulatedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                record.Value,
                record.BuildingId
            };

            _logger.LogInformation("Sending Record: {Record}", JsonSerializer.Serialize(emulatedRecord));

            if (_dataConnector is not DataConnector.Services.NullDataConnector)
            {
                await _dataConnector.SendMessageAsync(new Record
                {
                    Timestamp = emulatedTimestamp,
                    Value = record.Value,
                    BuildingId = record.BuildingId
                });
            }

            for (int i = 5; i <= 30; i += 5)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                _logger.LogInformation("{Minutes} minutes until the next record is sent.", 30 - i);
            }
        }
    }
}
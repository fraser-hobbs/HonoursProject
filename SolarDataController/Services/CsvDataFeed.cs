using System.Text.Json;
using DataConnector.Interfaces;
using SolarDataController.Helpers;
using SolarDataController.Interfaces;
using SolarDataController.Models;

namespace SolarDataController.Services;

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
        var rapidStart = Environment.GetEnvironmentVariable("RAPID_START") == "true";

        // Common alignment logic: find aligned start record based on current day of week and dataset
        var currentDayOfWeek = DateTime.UtcNow.DayOfWeek;
        var alignedDate = allRecords
            .Select(r => r.Timestamp.Date)
            .Distinct()
            .FirstOrDefault(d => d.DayOfWeek == currentDayOfWeek);

        if (alignedDate == default)
        {
            _logger.LogWarning("No matching day of week found in CSV for alignment.");
            alignedDate = allRecords.First().Timestamp.Date;
        }

        allRecords = allRecords.SkipWhile(r => r.Timestamp.Date < alignedDate).ToList();

        DateTime alignedNextLiveStart;

        if (rapidStart)
        {
            // Rapid start: start at 00:00 of 14 days ago, send 14 days of data with 30-minute intervals and 10-second delays
            var emulatedStartDate = DateTime.UtcNow.Date.AddDays(-14); // always 14 days ago
            var emulatedTimestamp = new DateTime(emulatedStartDate.Year, emulatedStartDate.Month, emulatedStartDate.Day, 0, 0, 0, DateTimeKind.Utc);
            var endEmulatedTimestamp = DateTime.UtcNow;
            _logger.LogInformation("Starting rapid start from {Start} to {End} (emulated range)", emulatedTimestamp, endEmulatedTimestamp);

            DateTime lastEmulatedTimestamp = emulatedTimestamp;

            alignedNextLiveStart = DateTime.UtcNow;
            int minute = alignedNextLiveStart.Minute < 30 ? 30 : 0;
            alignedNextLiveStart = new DateTime(alignedNextLiveStart.Year, alignedNextLiveStart.Month, alignedNextLiveStart.Day,
                                                alignedNextLiveStart.Hour, minute, 0, DateTimeKind.Utc);
            if (minute == 0)
                alignedNextLiveStart = alignedNextLiveStart.AddHours(1);

            while (emulatedTimestamp <= alignedNextLiveStart && !cancellationToken.IsCancellationRequested && allRecords.Any())
            {
                var record = allRecords.First();
                allRecords.RemoveAt(0);

                var emulatedRecord = new
                {
                    TimeStamp = emulatedTimestamp.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    record.Value,
                    record.ArrayId
                };

                _logger.LogInformation("Rapid Start - Sending Record: {Record}", JsonSerializer.Serialize(emulatedRecord));

                if (_dataConnector is not DataConnector.Services.NullDataConnector)
                {
                    await _dataConnector.SendMessageAsync(new Record
                    {
                        Timestamp = emulatedTimestamp,
                        Value = record.Value,
                        ArrayId = record.ArrayId
                    });
                }

                // Wait 10 seconds between sending records during rapid start
                // await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);

                lastEmulatedTimestamp = emulatedTimestamp;
                emulatedTimestamp = emulatedTimestamp.AddMinutes(30);
            }

            _logger.LogInformation("Rapid start data complete. Reverting to normal 30-minute interval mode.");

            // Set alignedNextLiveStart as the next 30-minute interval after lastEmulatedTimestamp
            var minutesPastHour = lastEmulatedTimestamp.Minute;
            if (minutesPastHour < 30)
                alignedNextLiveStart = new DateTime(lastEmulatedTimestamp.Year, lastEmulatedTimestamp.Month, lastEmulatedTimestamp.Day, lastEmulatedTimestamp.Hour, 30, 0, DateTimeKind.Utc);
            else
                alignedNextLiveStart = new DateTime(lastEmulatedTimestamp.Year, lastEmulatedTimestamp.Month, lastEmulatedTimestamp.Day, lastEmulatedTimestamp.Hour, 0, 0, DateTimeKind.Utc).AddHours(1);
        }
        else
        {
            // Non-rapid start: align start time based on current time rounded to nearest 30-minute interval on aligned date
            var nowUtc = DateTime.UtcNow;
            var baseDateTime = new DateTime(alignedDate.Year, alignedDate.Month, alignedDate.Day, nowUtc.Hour, nowUtc.Minute, 0, DateTimeKind.Utc);
            int minute = baseDateTime.Minute < 30 ? 0 : 30;
            alignedNextLiveStart = new DateTime(baseDateTime.Year, baseDateTime.Month, baseDateTime.Day, baseDateTime.Hour, minute, 0, DateTimeKind.Utc);
        }

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

                _logger.LogInformation("Array: {0} | Emulated Date: {1} | CSV Date: {2} {3:HH:mm} | Value: {4}",
                    record.ArrayId,
                    emulatedDateTime.ToString("dd/MM/yyyy HH:mm"),
                    record.Timestamp.ToString("dd/MM/yyyy"),
                    record.Timestamp,
                    record.Value);
            }
        }

        // PRODUCTION / LIVE LOGIC
        while (allRecords.Any() && !cancellationToken.IsCancellationRequested)
        {
            var record = allRecords.First();
            allRecords.RemoveAt(0);

            var emulatedRecord = new
            {
                TimeStamp = alignedNextLiveStart.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                record.Value,
                record.ArrayId
            };

            _logger.LogInformation("Sending Record: {Record}", JsonSerializer.Serialize(emulatedRecord));

            if (_dataConnector is not DataConnector.Services.NullDataConnector)
            {
                await _dataConnector.SendMessageAsync(new Record
                {
                    Timestamp = alignedNextLiveStart,
                    Value = record.Value,
                    ArrayId = record.ArrayId
                });
            }

            for (int i = 5; i <= 30; i += 5)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                _logger.LogInformation("{Minutes} minutes until the next record is sent.", 30 - i);
            }

            alignedNextLiveStart = alignedNextLiveStart.AddMinutes(30);
        }
    }
}
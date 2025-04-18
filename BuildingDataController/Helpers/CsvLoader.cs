using BuildingDataController.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace BuildingDataController.Helpers;

public class CsvLoader
{
    private readonly string _csvPath;
    private readonly string _sourceId;

    public CsvLoader(string csvPath, string sourceId)
    {
        _csvPath = csvPath;
        _sourceId = sourceId;
    }

    public List<Record> Load()
    {
        if (!File.Exists(_csvPath))
        {
            Console.WriteLine($"Fatal Error: CSV file not found at path: {_csvPath}");
            Environment.Exit(1);
        }

        var records = new List<Record>();

        using var reader = new StreamReader(_csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        });

        using var dr = new CsvDataReader(csv);
        var dt = new DataTable();
        dt.Load(dr);

        foreach (DataRow row in dt.Rows)
        {
            var dateStr = row["Date"].ToString();
            if (!DateTime.TryParse(dateStr, out var baseDate))
            {
                Console.WriteLine($"Skipping row with invalid date: {dateStr}");
                continue;
            }

            foreach (DataColumn col in dt.Columns)
            {
                var timeStr = col.ColumnName;
                if (timeStr == "Date" || timeStr == "Channel")
                    continue;

                if (!TimeSpan.TryParse(timeStr, out var timeOfDay))
                {
                    Console.WriteLine($"Skipping column with invalid time: {timeStr}");
                    continue;
                }

                var valueStr = row[timeStr]?.ToString();
                if (!double.TryParse(valueStr, out var value))
                {
                    Console.WriteLine($"Skipping value at {baseDate.Date + timeOfDay}: {valueStr}");
                    continue;
                }

                var timeStamp = RoundDownToNearestHalfHour(baseDate.Date.Add(timeOfDay));

                records.Add(new Record
                {
                    TimeStamp = timeStamp,
                    Value = value,
                    BuildingId = _sourceId
                });
            }
        }

        return records;
    }

    private DateTime RoundDownToNearestHalfHour(DateTime dt)
    {
        return new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute < 30 ? 0 : 30, 0);
    }
}
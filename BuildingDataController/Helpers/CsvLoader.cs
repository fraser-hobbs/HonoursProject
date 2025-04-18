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
        int rowIndex = 0;

        using var reader = new StreamReader(_csvPath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
        });

        using var dr = new CsvDataReader(csv);
        var dt = new DataTable();
        dt.Load(dr);

        // First column is assumed to be "Date", remaining are half-hour intervals (e.g., 00:00, 00:30, ...)
        foreach (DataRow row in dt.Rows)
        {
            rowIndex++;

            var dateStr = row[0]?.ToString();
            if (!DateTime.TryParse(dateStr, out var baseDate))
            {
                Console.WriteLine($"⚠️ Skipping row {rowIndex} with invalid date: '{dateStr}' in file '{_csvPath}'");
                Console.WriteLine($"  → Row contents: {string.Join(", ", row.ItemArray.Select(item => item?.ToString() ?? "[null]"))}");
                continue;
            }

            for (int colIndex = 1; colIndex < dt.Columns.Count; colIndex++)
            {
                var timeStr = dt.Columns[colIndex].ColumnName;
                if (!TimeSpan.TryParse(timeStr, out var timeOfDay))
                {
                    Console.WriteLine($"⚠️ Skipping column {colIndex} with invalid time header: '{timeStr}'");
                    continue;
                }

                var valueStr = row[colIndex]?.ToString();
                if (!double.TryParse(valueStr, out var value))
                {
                    Console.WriteLine($"⚠️ Skipping value at {baseDate:dd/MM/yyyy} {timeStr}: '{valueStr}'");
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
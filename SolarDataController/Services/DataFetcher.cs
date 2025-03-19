using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SolarDataController.Models;

namespace SolarDataController.Services
{
    public class DataFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DataFetcher> _logger;
        private static readonly string BaseUrl = "https://api.pvlive.uk/pvlive/api/v4/gsp/0";

        public DataFetcher(HttpClient httpClient, ILogger<DataFetcher> logger) 
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        
        public DataFetcher(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Record>> FetchSolarDataAsync()
        {
            DateTime endTime = DateTime.UtcNow;
            DateTime startTime = endTime.AddMinutes(-30);

            string start = startTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string end = endTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string apiUrl = $"{BaseUrl}?start={start}&end={end}";

            try
            {
                _logger.LogInformation("Fetching solar data from {Url}", apiUrl);
                var response = await _httpClient.GetAsync(apiUrl);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch data. HTTP Status: {StatusCode}", response.StatusCode);
                    return new List<Record>();
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                var document = JsonDocument.Parse(jsonResponse);
                var root = document.RootElement;

                var dataArray = root.GetProperty("data").EnumerateArray();
                var records = new List<Record>();

                foreach (var item in dataArray)
                {
                    var recordArray = item.EnumerateArray();
                    records.Add(new Record
                    {
                        GspId = recordArray.ElementAt(0).GetInt32(),
                        GmtTimestamp = DateTime.Parse(recordArray.ElementAt(1).GetString()!),
                        GenerationMw = recordArray.ElementAt(2).GetDouble()
                    });
                }

                _logger.LogInformation("Successfully fetched {Count} records", records.Count);
                return records;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching solar data");
                return new List<Record>();
            }
        }
    }
}
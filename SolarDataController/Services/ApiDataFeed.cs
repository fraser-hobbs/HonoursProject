using System.Net.Http;
using System.Text.Json;
using DataConnector.Interfaces;
using Microsoft.Extensions.Logging;
using SolarDataController.Models;
using SolarDataController.Interfaces;
using SolarDataController.Helpers;

namespace SolarDataController.Services
{
    public class ApiDataFeed : IDataFeedService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiDataFeed> _logger;
        private readonly IDataConnector _dataConnector;
        private static readonly string BaseUrl = "https://api.pvlive.uk/pvlive/api/v4/gsp/0";

        public ApiDataFeed(HttpClient httpClient, IDataConnector dataConnector, ILogger<ApiDataFeed> logger)
        {
            _httpClient = httpClient;
            _dataConnector = dataConnector;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
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
                        ArrayId = recordArray.ElementAt(0).ToString(),
                        Timestamp = DateTime.Parse(recordArray.ElementAt(1).GetString()!).ToUniversalTime(),
                        Value = recordArray.ElementAt(2).GetDouble()
                    });
                }

                _logger.LogInformation("Successfully fetched {Count} records", records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching solar data");
            }
        }
    }
}
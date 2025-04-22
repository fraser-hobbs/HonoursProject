using DataConnector.Config;
using DataConnector.Interfaces;
using DataConnector.Services;
using SolarDataController;
using SolarDataController.Helpers;
using SolarDataController.Interfaces;
using SolarDataController.Services;
using NullDataConnector = DataConnector.Services.NullDataConnector;

var isDevelopmentMode = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
var dataMode = Environment.GetEnvironmentVariable("DATA_MODE") ?? "csv";


var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((services) =>
    {
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddConsole();
        });


        if (!isDevelopmentMode)
        {
            var config = new DataConnectorConfig
            {
                KafkaServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
                KafkaTopic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "solar-data",
                ClientId = Environment.GetEnvironmentVariable("ARRAY_ID") ?? "solar-0x1"
            };
            services.AddSingleton(config);
            services.AddSingleton<IDataConnector, KafkaConnector>();
        }
        else
        {
            services.AddSingleton<IDataConnector, NullDataConnector>();
        }

        if (dataMode == "csv")
        {
            var csvPath = Environment.GetEnvironmentVariable("CSV_PATH") ?? "Data/SolarA.csv";
            var arrayId = Environment.GetEnvironmentVariable("ARRAY_ID") ?? "solar-0x1";
            services.AddSingleton(new CsvLoader(csvPath, arrayId));
            services.AddSingleton<IDataFeedService, CsvDataFeed>();
        }
        else
        {
            services.AddSingleton<IDataFeedService, ApiDataFeed>();
            services.AddHttpClient<ApiDataFeed>();
            services.AddSingleton<ApiDataFeed>();
        }

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
using BuildingDataController;
using BuildingDataController.Helpers;
using BuildingDataController.Services;
using DataConnector.Config;
using DataConnector.Interfaces;
using DataConnector.Services; 
using NullDataConnector = DataConnector.Services.NullDataConnector;

var isDevelopmentMode = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((services) =>
    {
        services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddConsole();
        });

        var dataMode = Environment.GetEnvironmentVariable("DATA_MODE") ?? "csv";

        if (!isDevelopmentMode)
        {
            var config = new DataConnectorConfig
            {
                KafkaServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
                KafkaTopic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "building-data",
                ClientId = Environment.GetEnvironmentVariable("BUILDING_ID") ?? "building-0x1"
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
            var csvPath = Environment.GetEnvironmentVariable("CSV_PATH") ?? "Data/BuildingA.csv";
            var buildingId = Environment.GetEnvironmentVariable("BUILDING_ID") ?? "building-0x1";
            services.AddSingleton(new CsvLoader(csvPath, buildingId));
            services.AddSingleton<IDataFeedService, CsvDataFeed>();
        }
        else
        {
            Console.WriteLine("Real Time Data Feed");
            // services.AddSingleton<IDataFeedService, RealTimeDataFeed>();
        }

        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
using DataConnector.Config;
using DataConnector.Interfaces;
using DataConnector.Services; // ✅ Added to resolve NullDataConnector
using SolarDataController;
using SolarDataController.Services;

var isDevelopmentMode = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";

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
                ClientId = Environment.GetEnvironmentVariable("Conn_Id") ?? "solar-0x1"
            };
            services.AddSingleton(config);
            services.AddSingleton<IDataConnector, KafkaConnector>();
        }
        else
        {
            services.AddSingleton<IDataConnector, NullDataConnector>(); // ✅ Now correctly recognized
        }

        services.AddHttpClient<DataFetcher>();
        services.AddSingleton<DataFetcher>();
        services.AddHostedService<SolarWorker>();
    })
    .Build();

await host.RunAsync();
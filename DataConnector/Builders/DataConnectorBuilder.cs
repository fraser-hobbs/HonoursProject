using Microsoft.Extensions.Logging;
using DataConnector.Config;
using DataConnector.Services;

namespace DataConnector.Builders
{
    public class DataConnectorBuilder
    {
        private readonly DataConnectorConfig _config;
        private ILogger<KafkaConnector>? _logger;

        public DataConnectorBuilder()
        {
            _config = new DataConnectorConfig
            {
                KafkaServer = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092",
                KafkaTopic = Environment.GetEnvironmentVariable("KAFKA_TOPIC") ?? "solar_data",
                ClientId = Environment.GetEnvironmentVariable("CLIENT_ID") ?? "0x0"
            };
        }

        public DataConnectorBuilder SetKafkaServer(string server)
        {
            _config.KafkaServer = server;
            return this;
        }

        public DataConnectorBuilder SetKafkaTopic(string topic)
        {
            _config.KafkaTopic = topic;
            return this;
        }

        public DataConnectorBuilder SetClientId(string clientId)
        {
            _config.ClientId = clientId;
            return this;
        }

        public DataConnectorBuilder SetLogLevel(LogLevel logLevel)
        {
            _config.LogLevel = logLevel;
            return this;
        }

        public DataConnectorBuilder SetLogger(ILogger<KafkaConnector> logger)
        {
            _logger = logger;
            return this;
        }

        public KafkaConnector Build()
        {
            if (string.IsNullOrEmpty(_config.KafkaServer) || 
                string.IsNullOrEmpty(_config.KafkaTopic) || 
                string.IsNullOrEmpty(_config.ClientId))
            {
                throw new InvalidOperationException("All required configuration properties must be set before building.");
            }

            if (_logger == null)
            {
                throw new InvalidOperationException("Logger must be set before building.");
            }

            return new KafkaConnector(_config, _logger);
        }
    }
}
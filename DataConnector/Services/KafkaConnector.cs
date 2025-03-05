using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using DataConnector.Config;
using DataConnector.Interfaces;
using DataConnector.Models;
using Serilog;

namespace DataConnector.Services
{
    public class KafkaConnector : IDataConnector
    {
        private readonly IProducer<string, string> _producer;
        private readonly DataConnectorConfig _config;

        public KafkaConnector(DataConnectorConfig config, IProducer<string, string>? producer = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _config.KafkaServer,
                ClientId = _config.ClientId
            };

            _producer = producer ?? new ProducerBuilder<string, string>(producerConfig).Build();

            Log.Debug("KafkaConnector initialized with configuration:\n{KafkaConfig}",
                JsonSerializer.Serialize(new
                {
                    _config.ClientId,
                    _config.KafkaServer,
                    _config.KafkaTopic
                }, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        public async Task SendMessageAsync<T>(KafkaMessage<T> message)
        {
            var jsonPayload = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                WriteIndented = true, // Pretty-print JSON
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var kafkaMessage = new Message<string, string>
            {
                Key = message.Source,
                Value = jsonPayload
            };

            try
            {
                var deliveryResult = await _producer.ProduceAsync(_config.KafkaTopic, kafkaMessage);

                if (_producer == null)
                {
                    throw new InvalidOperationException("KafkaProducer is null after initialization!");
                }

                Log.Debug("KafkaConnector initialized with producer: {Producer}", _producer);
                
                Log.Information("Message successfully sent to Kafka:\n{MessageDetails}",
                    JsonSerializer.Serialize(new
                    {
                        deliveryResult.Topic,
                        deliveryResult.Partition,
                        deliveryResult.Offset,
                        Key = message.Source,
                        Payload = message
                    }, new JsonSerializerOptions { WriteIndented = true })
                );
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending message to Kafka topic {Topic}", _config.KafkaTopic);
                throw;
            }
        }
    }
}
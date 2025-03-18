using System;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using DataConnector.Config;
using DataConnector.Interfaces;
using DataConnector.Models;
using Microsoft.Extensions.Logging;

namespace DataConnector.Services
{
    public class KafkaConnector : IDataConnector
    {
        private readonly IProducer<string, string> _producer;
        private readonly DataConnectorConfig _config;
        private readonly ILogger<KafkaConnector> _logger;

        public KafkaConnector(DataConnectorConfig config, ILogger<KafkaConnector> logger, IProducer<string, string>? producer = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _config.KafkaServer,
                ClientId = _config.ClientId
            };

            _producer = producer ?? new ProducerBuilder<string, string>(producerConfig).Build();

            _logger.LogDebug("KafkaConnector initialized with configuration:\n{KafkaConfig}",
                JsonSerializer.Serialize(new
                {
                    _config.ClientId,
                    _config.KafkaServer,
                    _config.KafkaTopic
                }, new JsonSerializerOptions { WriteIndented = true })
            );
        }

        public async Task SendMessageAsync(object payload)
        {
            var kafkaMessage = new KafkaMessage<object>
            {
                Source = _config.ClientId,
                Payload = payload
            };

            var jsonPayload = JsonSerializer.Serialize(kafkaMessage.Payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var messageToSend = new Message<string, string>
            {
                Key = kafkaMessage.Source,
                Value = jsonPayload
            };

            try
            {
                var deliveryResult = await _producer.ProduceAsync(_config.KafkaTopic, messageToSend);
                if (_producer == null)
                {
                    throw new InvalidOperationException("KafkaProducer is null after initialization!");
                }

                _logger.LogDebug("KafkaConnector initialized with producer: {Producer}", _producer);
                
                _logger.LogInformation("Message successfully sent to Kafka:\n{MessageDetails}",
                    JsonSerializer.Serialize(new
                    {
                        deliveryResult.Topic,
                        deliveryResult.Partition,
                        deliveryResult.Offset,
                        Key = kafkaMessage.Source,
                        Payload = kafkaMessage.Payload
                    }, new JsonSerializerOptions { WriteIndented = true })
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to Kafka topic {Topic}", _config.KafkaTopic);
                throw;
            }
        }
    }
}
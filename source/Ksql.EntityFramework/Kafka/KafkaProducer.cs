using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Ksql.EntityFramework.Configuration;

namespace Ksql.EntityFramework.Kafka;

/// <summary>
/// Kafka producer for producing messages to Kafka topics.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TValue">The type of the message value.</typeparam>
internal class KafkaProducer<TKey, TValue> : IDisposable where TValue : class
{
    private readonly IProducer<TKey, TValue> _producer;
    private readonly string _topic;
    private readonly KsqlDbContextOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaProducer{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="topic">The topic to produce messages to.</param>
    /// <param name="options">The options for the producer.</param>
    public KafkaProducer(string topic, KsqlDbContextOptions options)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var config = new ProducerConfig
        {
            BootstrapServers = ExtractBootstrapServers(options.ConnectionString),
            EnableDeliveryReports = true,
            ClientId = $"ksql-entityframework-producer-{Guid.NewGuid()}",
            // Add more producer configuration as needed
        };

        var schemaRegistryConfig = new SchemaRegistryConfig
        {
            Url = options.SchemaRegistryUrl
        };

        var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

        // Create the producer based on the value format
        switch (options.DefaultValueFormat)
        {
            case Models.ValueFormat.Avro:
                var avroSerializerConfig = new AvroSerializerConfig
                {
                    AutoRegisterSchemas = true
                };

                // For simplicity, we're using JsonSerializer for the key.
                // In a real implementation, you might want to use AvroSerializer for the key as well.
                _producer = new ProducerBuilder<TKey, TValue>(config)
                    .SetKeySerializer(new JsonSerializer<TKey>())
                    .SetValueSerializer(new AvroSerializer<TValue>(schemaRegistry, avroSerializerConfig))
                    .Build();
                break;

            case Models.ValueFormat.Json:
            default:
                _producer = new ProducerBuilder<TKey, TValue>(config)
                    .SetKeySerializer(new JsonSerializer<TKey>())
                    .SetValueSerializer(new JsonSerializer<TValue>())
                    .Build();
                break;
        }
    }

    /// <summary>
    /// Produces a single message to the topic.
    /// </summary>
    /// <param name="key">The key of the message.</param>
    /// <param name="value">The value of the message.</param>
    /// <returns>A task representing the asynchronous operation, with the result containing the delivery report.</returns>
    public async Task<DeliveryResult<TKey, TValue>> ProduceAsync(TKey key, TValue value)
    {
        var message = new Message<TKey, TValue>
        {
            Key = key,
            Value = value
        };

        try
        {
            return await _producer.ProduceAsync(_topic, message);
        }
        catch (ProduceException<TKey, TValue> ex)
        {
            Console.WriteLine($"Failed to deliver message: {ex.Error.Reason}");
            throw;
        }
    }

    /// <summary>
    /// Produces multiple messages to the topic in a batch.
    /// </summary>
    /// <param name="messages">The messages to produce.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProduceBatchAsync(IEnumerable<KeyValuePair<TKey, TValue>> messages)
    {
        var tasks = new List<Task<DeliveryResult<TKey, TValue>>>();

        foreach (var pair in messages)
        {
            tasks.Add(ProduceAsync(pair.Key, pair.Value));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Extracts the bootstrap servers from the connection string.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The bootstrap servers.</returns>
    private string ExtractBootstrapServers(string connectionString)
    {
        // In a real implementation, this would parse the connection string
        // For now, we assume the connection string is the bootstrap servers
        return connectionString;
    }

    /// <summary>
    /// Disposes the producer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the producer.
    /// </summary>
    /// <param name="disposing">Whether the method is being called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _producer.Dispose();
            }

            _disposed = true;
        }
    }
}
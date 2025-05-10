using System.Runtime.CompilerServices;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Kafka;

/// <summary>
/// Kafka consumer for consuming messages from Kafka topics.
/// </summary>
/// <typeparam name="TKey">The type of the message key.</typeparam>
/// <typeparam name="TValue">The type of the message value.</typeparam>
internal class KafkaConsumer<TKey, TValue> : IDisposable where TValue : class
{
    private readonly IConsumer<TKey, TValue> _consumer;
    private readonly string _topic;
    private readonly KsqlDbContextOptions _options;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaConsumer{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="topic">The topic to consume messages from.</param>
    /// <param name="options">The options for the consumer.</param>
    /// <param name="groupId">The consumer group ID.</param>
    public KafkaConsumer(string topic, KsqlDbContextOptions options, string groupId = null)
    {
        _topic = topic ?? throw new ArgumentNullException(nameof(topic));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var config = new ConsumerConfig
        {
            BootstrapServers = ExtractBootstrapServers(options.ConnectionString),
            GroupId = groupId ?? $"ksql-entityframework-consumer-{Guid.NewGuid()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true,
            // Add more consumer configuration as needed
        };

        var schemaRegistryConfig = new SchemaRegistryConfig
        {
            Url = options.SchemaRegistryUrl
        };


        var schemaRegistry = new CachedSchemaRegistryClient(schemaRegistryConfig);

        var builder = new ConsumerBuilder<TKey, TValue>(config)
                 .SetKeyDeserializer(new Confluent.SchemaRegistry.Serdes.AvroDeserializer<TKey>(schemaRegistry).AsSyncOverAsync())
                 .SetValueDeserializer(new Confluent.SchemaRegistry.Serdes.AvroDeserializer<TValue>(schemaRegistry).AsSyncOverAsync());

        _consumer = builder.Build();

        _consumer.Subscribe(_topic);
    }

    public IEnumerable<TValue> Consume([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));

            if (consumeResult != null && consumeResult.Message != null)
            {
                // Yield the message value
                yield return consumeResult.Message.Value;
            }

        }
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
    /// Handles consumer errors.
    /// </summary>
    /// <param name="consumer">The consumer.</param>
    /// <param name="error">The error.</param>
    private void OnConsumerError(IConsumer<TKey, TValue> consumer, Error error)
    {
        Console.WriteLine($"Consumer error: {error.Reason}");

        if (_options.DeserializationErrorPolicy == ErrorPolicy.Abort)
        {
            throw new KafkaException(error);
        }
    }

    /// <summary>
    /// Handles consume errors.
    /// </summary>
    /// <param name="ex">The consume exception.</param>
    private void HandleConsumeError(ConsumeException ex)
    {
        Console.WriteLine($"Consume error: {ex.Error.Reason}");

        if (_options.DeserializationErrorPolicy == ErrorPolicy.Abort)
        {
            throw ex;
        }
        // In a real implementation, we would handle the error according to the error policy
    }

    /// <summary>
    /// Disposes the consumer.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the consumer.
    /// </summary>
    /// <param name="disposing">Whether the method is being called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _consumer.Close();
                _consumer.Dispose();
            }

            _disposed = true;
        }
    }
}
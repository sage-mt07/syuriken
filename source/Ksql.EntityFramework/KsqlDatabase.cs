using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Schema;

namespace Ksql.EntityFramework;

/// <summary>
/// Implementation of database-level operations for a KSQL database.
/// </summary>
internal class KsqlDatabase : IKsqlDatabase, IDisposable, IAsyncDisposable
{
    private readonly KsqlDbContextOptions _options;
    private readonly SchemaManager _schemaManager;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlDatabase"/> class.
    /// </summary>
    /// <param name="options">The options for the database.</param>
    /// <param name="schemaManager">The schema manager for the database.</param>
    public KsqlDatabase(KsqlDbContextOptions options, SchemaManager schemaManager)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
    }

    /// <summary>
    /// Creates a table for the specified entity type with the given configuration options.
    /// </summary>
    /// <typeparam name="T">The type of entity for the table.</typeparam>
    /// <param name="tableName">The name of the table to create.</param>
    /// <param name="options">A function to configure the table options.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task CreateTableAsync<T>(string tableName, Func<TableOptions, TableOptions> options) where T : class
    {
        var tableOptions = options(new TableOptions());
        var topicDescriptor = _schemaManager.GetTopicDescriptor<T>();

        var ksql = GenerateCreateTableStatement(tableName, topicDescriptor, tableOptions);
        await ExecuteKsqlAsync(ksql).ConfigureAwait(false);
    }

    /// <summary>
    /// Drops a table with the specified name.
    /// </summary>
    /// <param name="tableName">The name of the table to drop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DropTableAsync(string tableName)
    {
        var ksql = $"DROP TABLE {tableName} DELETE TOPIC;";
        return ExecuteKsqlAsync(ksql);
    }

    /// <summary>
    /// Drops a topic with the specified name.
    /// </summary>
    /// <param name="topicName">The name of the topic to drop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DropTopicAsync(string topicName)
    {
        // This is a placeholder implementation as dropping topics is typically done through Kafka admin APIs
        // In a real implementation, this would use the Kafka Admin Client to delete the topic
        return Task.CompletedTask;
    }

    /// <summary>
    /// Executes a KSQL statement directly.
    /// </summary>
    /// <param name="ksqlStatement">The KSQL statement to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteKsqlAsync(string ksqlStatement)
    {
        Console.WriteLine($"Executing KSQL: {ksqlStatement}");

        using var ksqlClient = new Ksql.KsqlClient(_options.ConnectionString);
        await ksqlClient.ExecuteKsqlAsync(ksqlStatement);
    }

    internal async Task EnsureTopicCreatedAsync(TopicDescriptor topicDescriptor)
    {
        if (topicDescriptor == null)
            throw new ArgumentNullException(nameof(topicDescriptor));

        if (string.IsNullOrEmpty(topicDescriptor.Name))
            throw new ArgumentException("Topic name cannot be null or empty", nameof(topicDescriptor));

        // Extract bootstrap servers from connection string
        string bootstrapServers = ExtractBootstrapServers(_options.ConnectionString);
        if (string.IsNullOrEmpty(bootstrapServers))
            throw new InvalidOperationException("Could not extract bootstrap servers from connection string");

        // Create admin client config
        var adminConfig = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers
        };

        // Create admin client
        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        try
        {
            // Get metadata to check if topic exists
            var metadata = adminClient.GetMetadata(
                topicDescriptor.Name,
                TimeSpan.FromSeconds(_options.ConnectionTimeoutSeconds));

            var topicExists = metadata.Topics.Any(t => t.Topic == topicDescriptor.Name);

            if (!topicExists)
            {
                // Create topic specification
                var topicSpec = new TopicSpecification
                {
                    Name = topicDescriptor.Name,
                    NumPartitions = topicDescriptor.PartitionCount,
                    ReplicationFactor = (short)topicDescriptor.ReplicationFactor
                };

                // Create the topic
                await adminClient.CreateTopicsAsync(new[] { topicSpec });
                Console.WriteLine($"Created topic: {topicDescriptor.Name}");
            }
            else
            {
                Console.WriteLine($"Topic already exists: {topicDescriptor.Name}");
            }
        }
        catch (KafkaException ex)
        {
            Console.WriteLine($"Error creating topic: {ex.Message}");
            throw;
        }
    }

    private string ExtractBootstrapServers(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return string.Empty;

        // If it's a mock URL, extract the host and port
        if (connectionString.StartsWith("mock://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString.Substring(7);
        }

        // If it starts with http:// or https://, remove the protocol
        if (connectionString.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString.Substring(7);
        }

        if (connectionString.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString.Substring(8);
        }

        // Otherwise return as is
        return connectionString;
    }
    /// <summary>
    /// Ensures that a stream exists for the specified topic descriptor.
    /// </summary>
    /// <param name="topicDescriptor">The descriptor for the topic.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task EnsureStreamCreatedAsync(TopicDescriptor topicDescriptor)
    {
        // First ensure the topic exists
        await EnsureTopicCreatedAsync(topicDescriptor).ConfigureAwait(false);

        // Then create the stream using KSQL
        var ksql = GenerateCreateStreamStatement(topicDescriptor);
        await ExecuteKsqlAsync(ksql).ConfigureAwait(false);
    }

    /// <summary>
    /// Ensures that a table exists for the specified table descriptor.
    /// </summary>
    /// <param name="tableDescriptor">The descriptor for the table.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal async Task EnsureTableCreatedAsync(TableDescriptor tableDescriptor)
    {
        // First ensure the topic exists
        await EnsureTopicCreatedAsync(tableDescriptor.TopicDescriptor).ConfigureAwait(false);

        // Then create the table using KSQL
        var ksql = GenerateCreateTableStatement(tableDescriptor.Name, tableDescriptor.TopicDescriptor, tableDescriptor.Options);
        await ExecuteKsqlAsync(ksql).ConfigureAwait(false);
    }

    /// <summary>
    /// Begins a transaction on this database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, with the result containing the transaction.</returns>
    internal Task<IKsqlTransaction> BeginTransactionAsync()
    {
        // This is a placeholder implementation for beginning a transaction
        // In a real implementation, this would create a transaction object that can be used to commit or abort changes
        var transaction = new KsqlTransaction();
        return Task.FromResult<IKsqlTransaction>(transaction);
    }

    /// <summary>
    /// Refreshes the metadata for the database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    internal Task RefreshMetadataAsync()
    {
        // This is a placeholder implementation for refreshing metadata
        // In a real implementation, this would refresh the metadata for streams and tables from the KSQL server
        return Task.CompletedTask;
    }

    private string GenerateCreateStreamStatement(TopicDescriptor topicDescriptor)
    {
        var schema = _schemaManager.GetSchemaString(topicDescriptor.EntityType);
        var valueFormat = topicDescriptor.ValueFormat.ToString().ToUpperInvariant();

        var ksql = $@"CREATE STREAM IF NOT EXISTS {topicDescriptor.EntityType.Name.ToLowerInvariant()} (
{schema}
) WITH (
  KAFKA_TOPIC = '{topicDescriptor.Name}',
  VALUE_FORMAT = '{valueFormat}'";

        // 複合キーの処理
        if (topicDescriptor.KeyColumns.Count > 0)
        {
            ksql += $",  KEY = '{string.Join("', '", topicDescriptor.KeyColumns)}'";
        }

        if (topicDescriptor.TimestampColumn != null)
        {
            ksql += $",  TIMESTAMP = '{topicDescriptor.TimestampColumn}'";

            if (topicDescriptor.TimestampFormat != null)
            {
                ksql += $",  TIMESTAMP_FORMAT = '{topicDescriptor.TimestampFormat}'";
            }
        }

        ksql += ");";

        return ksql;
    }

    private string GenerateCreateTableStatement(string tableName, TopicDescriptor topicDescriptor, TableOptions options)
    {
        var schema = _schemaManager.GetSchemaString(topicDescriptor.EntityType);
        var valueFormat = options.ValueFormat.ToString().ToUpperInvariant();

        var ksql = $@"CREATE TABLE IF NOT EXISTS {tableName} (
{schema}
) WITH (
  KAFKA_TOPIC = '{options.TopicName ?? topicDescriptor.Name}',
  VALUE_FORMAT = '{valueFormat}'";

        // 複合キーの処理
        var keyColumnsList = options.KeyColumns.Count > 0
            ? options.KeyColumns
            : topicDescriptor.KeyColumns;

        if (keyColumnsList.Count > 0)
        {
            ksql += $",  KEY = '{string.Join("', '", keyColumnsList)}'";
        }


        if (options.TimestampColumn != null)
        {
            ksql += $",  TIMESTAMP = '{options.TimestampColumn}'";

            if (options.TimestampFormat != null)
            {
                ksql += $",  TIMESTAMP_FORMAT = '{options.TimestampFormat}'";
            }
        }
        else if (topicDescriptor.TimestampColumn != null)
        {
            ksql += $",  TIMESTAMP = '{topicDescriptor.TimestampColumn}'";

            if (topicDescriptor.TimestampFormat != null)
            {
                ksql += $",  TIMESTAMP_FORMAT = '{topicDescriptor.TimestampFormat}'";
            }
        }

        ksql += ");";

        return ksql;
    }

    /// <summary>
    /// Disposes the database.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the database asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public ValueTask DisposeAsync()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Disposes the database.
    /// </summary>
    /// <param name="disposing">Whether the method is being called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Dispose managed state (managed objects)
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            // Set large fields to null
            _disposed = true;
        }
    }
}

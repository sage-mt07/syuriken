using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Configuration;

/// <summary>
/// Builder for configuring KSQL database context options.
/// </summary>
public class KsqlDbContextOptionsBuilder
{
    private readonly KsqlDbContextOptions _options = new KsqlDbContextOptions();

    /// <summary>
    /// Specifies the connection string to the KSQL server.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseConnectionString(string connectionString)
    {
        _options.ConnectionString = connectionString;
        return this;
    }

    /// <summary>
    /// Specifies the schema registry URL.
    /// </summary>
    /// <param name="url">The schema registry URL.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseSchemaRegistry(string url)
    {
        _options.SchemaRegistryUrl = url;
        return this;
    }

    /// <summary>
    /// Specifies the default value format for topics.
    /// </summary>
    /// <param name="format">The default value format.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseDefaultValueFormat(ValueFormat format)
    {
        _options.DefaultValueFormat = format;
        return this;
    }

    /// <summary>
    /// Specifies the policy for handling deserialization errors.
    /// </summary>
    /// <param name="policy">The error policy.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseDeserializationErrorPolicy(ErrorPolicy policy)
    {
        _options.DeserializationErrorPolicy = policy;
        return this;
    }

    /// <summary>
    /// Configures a dead letter queue for handling errors.
    /// </summary>
    /// <param name="topicName">The name of the dead letter queue topic.</param>
    /// <param name="errorHandler">A function that creates a dead letter message from data and an error.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseDeadLetterQueue(string topicName, Func<byte[]?, Exception, DeadLetterMessage>? errorHandler = null)
    {
        _options.DeadLetterQueue = topicName;
        _options.DeadLetterQueueErrorHandler = errorHandler;
        return this;
    }

    /// <summary>
    /// Specifies the default number of partitions for new topics.
    /// </summary>
    /// <param name="partitionCount">The default partition count.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseDefaultPartitionCount(int partitionCount)
    {
        _options.DefaultPartitionCount = partitionCount;
        return this;
    }

    /// <summary>
    /// Specifies the default replication factor for new topics.
    /// </summary>
    /// <param name="replicationFactor">The default replication factor.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseDefaultReplicationFactor(int replicationFactor)
    {
        _options.DefaultReplicationFactor = replicationFactor;
        return this;
    }

    /// <summary>
    /// Specifies the connection timeout in seconds.
    /// </summary>
    /// <param name="timeoutSeconds">The connection timeout in seconds.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseConnectionTimeout(int timeoutSeconds)
    {
        _options.ConnectionTimeoutSeconds = timeoutSeconds;
        return this;
    }

    /// <summary>
    /// Specifies the retry settings for operations.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retries.</param>
    /// <param name="backoffMs">The retry backoff in milliseconds.</param>
    /// <returns>The options builder for method chaining.</returns>
    public KsqlDbContextOptionsBuilder UseRetryPolicy(int maxRetries, int backoffMs)
    {
        _options.MaxRetries = maxRetries;
        _options.RetryBackoffMs = backoffMs;
        return this;
    }

    /// <summary>
    /// Builds the KSQL database context options.
    /// </summary>
    /// <returns>The configured options.</returns>
    public KsqlDbContextOptions Build()
    {
        return _options;
    }
}

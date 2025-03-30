using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Configuration
{
    /// <summary>
    /// Options for configuring a KSQL database context.
    /// </summary>
    public class KsqlDbContextOptions
    {
        /// <summary>
        /// Gets or sets the connection string to the KSQL server.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the schema registry URL.
        /// </summary>
        public string? SchemaRegistryUrl { get; set; }

        /// <summary>
        /// Gets or sets the default value format for topics.
        /// </summary>
        public ValueFormat DefaultValueFormat { get; set; } = ValueFormat.Avro;

        /// <summary>
        /// Gets or sets the policy for handling deserialization errors.
        /// </summary>
        public ErrorPolicy DeserializationErrorPolicy { get; set; } = ErrorPolicy.Abort;

        /// <summary>
        /// Gets or sets the name of the dead letter queue topic.
        /// </summary>
        public string? DeadLetterQueue { get; set; }

        /// <summary>
        /// Gets or sets a function that creates a dead letter message from data and an error.
        /// </summary>
        public Func<byte[]?, Exception, DeadLetterMessage>? DeadLetterQueueErrorHandler { get; set; }

        /// <summary>
        /// Gets or sets the default number of partitions for new topics.
        /// </summary>
        public int DefaultPartitionCount { get; set; } = 3;

        /// <summary>
        /// Gets or sets the default replication factor for new topics.
        /// </summary>
        public int DefaultReplicationFactor { get; set; } = 3;

        /// <summary>
        /// Gets or sets the connection timeout in seconds.
        /// </summary>
        public int ConnectionTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Gets or sets the maximum number of retries for operations.
        /// </summary>
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Gets or sets the retry backoff in milliseconds.
        /// </summary>
        public int RetryBackoffMs { get; set; } = 500;
    }
}

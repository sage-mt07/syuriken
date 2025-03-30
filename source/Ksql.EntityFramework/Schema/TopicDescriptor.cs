using System;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Schema
{
    /// <summary>
    /// Describes a Kafka topic and its associated schema.
    /// </summary>
    internal class TopicDescriptor
    {
        /// <summary>
        /// Gets or sets the name of the topic.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity type for the topic.
        /// </summary>
        public Type EntityType { get; set; } = typeof(object);

        /// <summary>
        /// Gets or sets the number of partitions for the topic.
        /// </summary>
        public int PartitionCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the replication factor for the topic.
        /// </summary>
        public int ReplicationFactor { get; set; } = 1;

        /// <summary>
        /// Gets or sets the key column for the topic.
        /// </summary>
        public string? KeyColumn { get; set; }

        /// <summary>
        /// Gets or sets the timestamp column for the topic.
        /// </summary>
        public string? TimestampColumn { get; set; }

        /// <summary>
        /// Gets or sets the format for the timestamp column.
        /// </summary>
        public string? TimestampFormat { get; set; }

        /// <summary>
        /// Gets or sets the format for values in the topic.
        /// </summary>
        public ValueFormat ValueFormat { get; set; } = ValueFormat.Avro;
    }
}

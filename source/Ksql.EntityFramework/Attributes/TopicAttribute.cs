using System;

namespace Ksql.EntityFramework.Attributes
{
    /// <summary>
    /// Specifies that a class represents a Kafka topic.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TopicAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the name of the topic.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the number of partitions for the topic.
        /// </summary>
        public int PartitionCount { get; set; } = 1;

        /// <summary>
        /// Gets or sets the replication factor for the topic.
        /// </summary>
        public int ReplicationFactor { get; set; } = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="TopicAttribute"/> class with the specified topic name.
        /// </summary>
        /// <param name="name">The name of the topic.</param>
        public TopicAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }
    }
}

using System;
using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Examples.Models
{
    /// <summary>
    /// Represents an order in the system.
    /// </summary>
    [Topic("orders", PartitionCount = 12, ReplicationFactor = 3)]
    public class Order
    {
        /// <summary>
        /// Gets or sets the ID of the order.
        /// </summary>
        [Key]
        public string OrderId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the customer who placed the order.
        /// </summary>
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the amount of the order.
        /// </summary>
        [DecimalPrecision(18, 2)]
        public decimal Amount { get; set; }

        /// <summary>
        /// Gets or sets the time when the order was placed.
        /// </summary>
        [Timestamp(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Type = TimestampType.EventTime)]
        public DateTimeOffset OrderTime { get; set; }
    }
}

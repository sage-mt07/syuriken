using System;
using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Tests.Models
{
    [Topic("test_orders", PartitionCount = 3, ReplicationFactor = 1)]
    public class TestOrder
    {
        [Key]
        public string OrderId { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        [DecimalPrecision(18, 2)]
        public decimal Amount { get; set; }

        [Timestamp(Format = "yyyy-MM-dd'T'HH:mm:ss.SSS", Type = TimestampType.EventTime)]
        public DateTimeOffset OrderTime { get; set; }
    }
}
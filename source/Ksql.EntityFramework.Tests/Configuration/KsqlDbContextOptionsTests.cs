using System;
using Xunit;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Tests.Configuration
{
    public class KsqlDbContextOptionsTests
    {
        [Fact]
        public void KsqlDbContextOptions_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var options = new KsqlDbContextOptions();

            // Assert
            Assert.Null(options.ConnectionString);
            Assert.Null(options.SchemaRegistryUrl);
            Assert.Equal(ValueFormat.Avro, options.DefaultValueFormat);
            Assert.Equal(ErrorPolicy.Abort, options.DeserializationErrorPolicy);
            Assert.Null(options.DeadLetterQueue);
            Assert.Null(options.DeadLetterQueueErrorHandler);
            Assert.Equal(3, options.DefaultPartitionCount);
            Assert.Equal(3, options.DefaultReplicationFactor);
            Assert.Equal(30, options.ConnectionTimeoutSeconds);
            Assert.Equal(3, options.MaxRetries);
            Assert.Equal(500, options.RetryBackoffMs);
        }

        [Fact]
        public void KsqlDbContextOptionsBuilder_ShouldBuildOptionsWithCustomValues()
        {
            // Arrange
            var builder = new KsqlDbContextOptionsBuilder();
            var errorHandler = new Func<byte[]?, Exception, DeadLetterMessage>((data, ex) => new DeadLetterMessage());

            // Act
            var options = builder
                .UseConnectionString("localhost:9092")
                .UseSchemaRegistry("localhost:8081")
                .UseDefaultValueFormat(ValueFormat.Json)
                .UseDeserializationErrorPolicy(ErrorPolicy.Skip)
                .UseDeadLetterQueue("error_topic", errorHandler)
                .UseDefaultPartitionCount(5)
                .UseDefaultReplicationFactor(2)
                .UseConnectionTimeout(60)
                .UseRetryPolicy(5, 1000)
                .Build();

            // Assert
            Assert.Equal("localhost:9092", options.ConnectionString);
            Assert.Equal("localhost:8081", options.SchemaRegistryUrl);
            Assert.Equal(ValueFormat.Json, options.DefaultValueFormat);
            Assert.Equal(ErrorPolicy.Skip, options.DeserializationErrorPolicy);
            Assert.Equal("error_topic", options.DeadLetterQueue);
            Assert.Same(errorHandler, options.DeadLetterQueueErrorHandler);
            Assert.Equal(5, options.DefaultPartitionCount);
            Assert.Equal(2, options.DefaultReplicationFactor);
            Assert.Equal(60, options.ConnectionTimeoutSeconds);
            Assert.Equal(5, options.MaxRetries);
            Assert.Equal(1000, options.RetryBackoffMs);
        }
    }
}
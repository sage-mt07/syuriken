using System;
using Xunit;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Tests.Models;

namespace Ksql.EntityFramework.Tests.Configuration
{
    public class TableConfigurationTests
    {
        [Fact]
        public void TableOptions_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var options = new TableOptions();

            // Assert
            Assert.Null(options.TopicName);
            Assert.Equal(ValueFormat.Avro, options.ValueFormat);
            Assert.Empty(options.KeyColumns);
            Assert.Null(options.PartitionBy);
            Assert.Null(options.TimestampColumn);
            Assert.Null(options.TimestampFormat);
        }

        [Fact]
        public void TableOptions_ShouldSetPropertiesWithFluentApi()
        {
            // Arrange
            var options = new TableOptions();

            // Act
            options
                .WithTopic("test_topic")
                .WithValueFormat(ValueFormat.Json)
                .WithKeyColumns("id", "name")
                .WithPartitionBy("id")
                .WithTimestamp("create_time", "yyyy-MM-dd");

            // Assert
            Assert.Equal("test_topic", options.TopicName);
            Assert.Equal(ValueFormat.Json, options.ValueFormat);
            Assert.Equal(2, options.KeyColumns.Count);
            Assert.Contains("id", options.KeyColumns);
            Assert.Contains("name", options.KeyColumns);
            Assert.Equal("id", options.PartitionBy);
            Assert.Equal("create_time", options.TimestampColumn);
            Assert.Equal("yyyy-MM-dd", options.TimestampFormat);
        }

        [Fact]
        public void TableBuilder_ShouldCreateTableConfiguration()
        {
            // Arrange
            var builder = new TableBuilder<TestCustomer>("test_customers");

            // Act
            builder
                .FromTopic<TestCustomer>("test_customers_topic")
                .WithKeyColumn(c => c.CustomerId)
                .WithValueFormat(ValueFormat.Json)
                .WithTimestamp(c => c.TotalPurchases, "N2");

            var options = builder.Build();
            var source = builder.GetSource();

            // Assert
            Assert.Equal("test_customers_topic", options.TopicName);
            Assert.Equal(ValueFormat.Json, options.ValueFormat);
            Assert.Single(options.KeyColumns);
            Assert.Contains("CustomerId", options.KeyColumns);
            Assert.Equal("TotalPurchases", options.TimestampColumn);
            Assert.Equal("N2", options.TimestampFormat);
            Assert.Null(source.StreamSource);
            Assert.Equal("test_customers_topic", source.TopicSource);
        }

        [Fact]
        public void TableBuilder_ShouldThrowException_WhenNameIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TableBuilder<TestCustomer>(null));
        }

        [Fact]
        public void TableBuilder_WithKeyColumn_ShouldThrowException_WhenSelectorIsNotPropertyAccess()
        {
            // Arrange
            var builder = new TableBuilder<TestCustomer>("test_customers");

            // Act & Assert
            Assert.Throws<ArgumentException>(() => builder.WithKeyColumn(c => "not-a-property"));
        }
    }
}
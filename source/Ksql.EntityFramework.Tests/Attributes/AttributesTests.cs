using System;
using Xunit;
using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Tests.Attributes
{
    public class AttributesTests
    {
        [Fact]
        public void TopicAttribute_ShouldSetProperties()
        {
            // Arrange & Act
            var attr = new TopicAttribute("test_topic")
            {
                PartitionCount = 5,
                ReplicationFactor = 3
            };

            // Assert
            Assert.Equal("test_topic", attr.Name);
            Assert.Equal(5, attr.PartitionCount);
            Assert.Equal(3, attr.ReplicationFactor);
        }

        [Fact]
        public void TopicAttribute_ShouldThrowException_WhenNameIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TopicAttribute(null));
        }

        [Fact]
        public void KeyAttribute_ShouldBeCreated()
        {
            // Arrange & Act
            var attr = new KeyAttribute();

            // Assert
            Assert.NotNull(attr);
        }

        [Fact]
        public void TimestampAttribute_ShouldSetProperties()
        {
            // Arrange & Act
            var attr = new TimestampAttribute
            {
                Format = "yyyy-MM-dd",
                Type = TimestampType.EventTime
            };

            // Assert
            Assert.Equal("yyyy-MM-dd", attr.Format);
            Assert.Equal(TimestampType.EventTime, attr.Type);
        }

        [Fact]
        public void TimestampAttribute_DefaultType_ShouldBeEventTime()
        {
            // Arrange & Act
            var attr = new TimestampAttribute();

            // Assert
            Assert.Equal(TimestampType.EventTime, attr.Type);
        }

        [Fact]
        public void DecimalPrecisionAttribute_ShouldSetProperties()
        {
            // Arrange & Act
            var attr = new DecimalPrecisionAttribute(18, 2);

            // Assert
            Assert.Equal(18, attr.Precision);
            Assert.Equal(2, attr.Scale);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(-1, 0)]
        public void DecimalPrecisionAttribute_ShouldThrowException_WhenPrecisionIsInvalid(int precision, int scale)
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalPrecisionAttribute(precision, scale));
        }

        [Theory]
        [InlineData(5, 6)]
        [InlineData(5, -1)]
        public void DecimalPrecisionAttribute_ShouldThrowException_WhenScaleIsInvalid(int precision, int scale)
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => new DecimalPrecisionAttribute(precision, scale));
        }

        [Fact]
        public void DefaultValueAttribute_ShouldSetValue()
        {
            // Arrange & Act
            var attr = new DefaultValueAttribute(42);

            // Assert
            Assert.Equal(42, attr.Value);
        }

        [Fact]
        public void DateTimeFormatAttribute_ShouldSetProperties()
        {
            // Arrange & Act
            var attr = new DateTimeFormatAttribute
            {
                Format = "yyyy-MM-dd",
                Locale = "en-US"
            };

            // Assert
            Assert.Equal("yyyy-MM-dd", attr.Format);
            Assert.Equal("en-US", attr.Locale);
        }
    }
}
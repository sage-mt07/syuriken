using System;
using Xunit;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework.Tests.Windows
{
    public class WindowTests
    {
        [Fact]
        public void TumblingWindow_ShouldSetProperties()
        {
            // Arrange
            var timeSpan = TimeSpan.FromMinutes(5);

            // Act
            var window = new TumblingWindow(timeSpan);

            // Assert
            Assert.Equal(timeSpan, window.Size);
            Assert.Equal(WindowType.Tumbling, window.WindowType);
        }

        [Fact]
        public void TumblingWindow_Of_ShouldCreateInstance()
        {
            // Arrange
            var timeSpan = TimeSpan.FromMinutes(5);

            // Act
            var window = TumblingWindow.Of(timeSpan);

            // Assert
            Assert.Equal(timeSpan, window.Size);
            Assert.Equal(WindowType.Tumbling, window.WindowType);
        }

        [Theory]
        [InlineData(500, "TUMBLING (SIZE 500 MILLISECONDS)")]
        [InlineData(1500, "TUMBLING (SIZE 1 SECONDS)")]
        [InlineData(60000, "TUMBLING (SIZE 1 MINUTES)")]
        [InlineData(3600000, "TUMBLING (SIZE 1 HOURS)")]
        [InlineData(86400000, "TUMBLING (SIZE 1 DAYS)")]
        public void TumblingWindow_ToKsqlString_ShouldGenerateCorrectString(int milliseconds, string expected)
        {
            // Arrange
            var timeSpan = TimeSpan.FromMilliseconds(milliseconds);
            var window = new TumblingWindow(timeSpan);

            // Act
            var result = window.ToKsqlString();

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
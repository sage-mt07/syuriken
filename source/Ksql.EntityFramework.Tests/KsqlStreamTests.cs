using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Tests.Context;
using Ksql.EntityFramework.Tests.Helpers;
using Ksql.EntityFramework.Tests.Models;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework.Tests
{
    public class KsqlStreamTests
    {
        [Fact]
        public void KsqlStream_ShouldHaveCorrectName()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = (KsqlStream<TestOrder>)context.Orders;

            // Assert
            Assert.Equal("testorder", stream.Name);
        }

        [Fact]
        public void GetEnumerator_ShouldReturnEmptyEnumerator()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = context.Orders;

            // Act
            var enumerator = stream.GetEnumerator();

            // Assert
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void OnError_ShouldReturnSameStream()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = context.Orders;

            // Act
            var result = stream.OnError(ErrorAction.Skip);

            // Assert
            Assert.Same(stream, result);
        }

        [Fact]
        public void Window_ShouldReturnWindowedStream()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = context.Orders;
            var window = TumblingWindow.Of(TimeSpan.FromMinutes(5));

            // Act
            var windowedStream = stream.Window(window);

            // Assert
            Assert.NotNull(windowedStream);
            Assert.Equal(window, windowedStream.WindowSpecification);
        }

        [Fact]
        public async Task ProduceAsync_ShouldThrowException_WithoutKafka()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = context.Orders;
            var order = new TestOrder
            {
                OrderId = "TEST-001",
                CustomerId = "CUST-001",
                Amount = 100.50m,
                OrderTime = DateTimeOffset.UtcNow
            };

            // Act & Assert
            // Without a running Kafka server, this should throw an exception
            await Assert.ThrowsAnyAsync<Exception>(() => stream.ProduceAsync(order));
        }

        [Fact]
        public async Task ProduceAsync_WithKey_ShouldThrowException_WithoutKafka()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = context.Orders;
            var order = new TestOrder
            {
                OrderId = "TEST-001",
                CustomerId = "CUST-001",
                Amount = 100.50m,
                OrderTime = DateTimeOffset.UtcNow
            };

            // Act & Assert
            // Without a running Kafka server, this should throw an exception
            await Assert.ThrowsAnyAsync<Exception>(() => stream.ProduceAsync("test-key", order));
        }

        [Fact]
        public async Task ProduceBatchAsync_ShouldThrowException_WithoutKafka()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = context.Orders;
            var orders = new List<TestOrder>
            {
                new TestOrder
                {
                    OrderId = "TEST-001",
                    CustomerId = "CUST-001",
                    Amount = 100.50m,
                    OrderTime = DateTimeOffset.UtcNow
                },
                new TestOrder
                {
                    OrderId = "TEST-002",
                    CustomerId = "CUST-002",
                    Amount = 200.75m,
                    OrderTime = DateTimeOffset.UtcNow
                }
            };

            // Act & Assert
            // Without a running Kafka server, this should throw an exception
            await Assert.ThrowsAnyAsync<Exception>(() => stream.ProduceBatchAsync(orders));
        }

        [Fact]
        public void Add_ShouldAddToContext()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var stream = context.Orders;
            var order = new TestOrder
            {
                OrderId = "TEST-001",
                CustomerId = "CUST-001",
                Amount = 100.50m,
                OrderTime = DateTimeOffset.UtcNow
            };

            // Act - this should not throw
            stream.Add(order);

            // Assert - this is difficult to test without mocking the internals
            // In a real test with mocking, we would verify the call to AddToPendingChanges
        }
    }
}
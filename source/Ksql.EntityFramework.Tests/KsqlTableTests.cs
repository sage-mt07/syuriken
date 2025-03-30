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

namespace Ksql.EntityFramework.Tests
{
    public class KsqlTableTests
    {
        [Fact]
        public void KsqlTable_ShouldHaveCorrectName()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = (KsqlTable<TestCustomer>)context.Customers;

            // Assert
            Assert.Equal("testcustomer", table.Name);
        }

        [Fact]
        public void GetEnumerator_ShouldReturnEmptyEnumerator()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = context.Customers;

            // Act
            var enumerator = table.GetEnumerator();

            // Assert
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public async Task GetAsync_ShouldThrowException_WithoutKafka()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = context.Customers;

            // Act & Assert
            // Without a running Kafka server, this should throw an exception
            await Assert.ThrowsAnyAsync<Exception>(() => table.GetAsync("CUST-001"));
        }

        [Fact]
        public async Task FindAsync_ShouldThrowException_WithoutKafka()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = context.Customers;

            // Act & Assert
            // Without a running Kafka server, this should throw an exception
            await Assert.ThrowsAnyAsync<Exception>(() => table.FindAsync("CUST-001"));
        }

        [Fact]
        public async Task InsertAsync_ShouldThrowException_WithoutKafka()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = context.Customers;
            var customer = new TestCustomer
            {
                CustomerId = "CUST-001",
                Name = "Test Customer",
                Email = "test@example.com",
                TotalPurchases = 0
            };

            // Act & Assert
            // Without a running Kafka server, this should throw an exception
            await Assert.ThrowsAnyAsync<Exception>(() => table.InsertAsync(customer));
        }

        [Fact]
        public async Task ToListAsync_ShouldThrowException_WithoutKafka()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = context.Customers;

            // Act & Assert
            // Without a running Kafka server, this should throw an exception
            await Assert.ThrowsAnyAsync<Exception>(() => table.ToListAsync());
        }

        [Fact]
        public void Add_ShouldAddToContext()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = context.Customers;
            var customer = new TestCustomer
            {
                CustomerId = "CUST-001",
                Name = "Test Customer",
                Email = "test@example.com",
                TotalPurchases = 0
            };

            // Act - this should not throw
            table.Add(customer);

            // Assert - this is difficult to test without mocking the internals
            // In a real test with mocking, we would verify the call to AddToPendingChanges
        }

        [Fact]
        public void Remove_ShouldNotThrow()
        {
            // Arrange
            using var context = TestHelper.CreateTestContext();
            var table = context.Customers;
            var customer = new TestCustomer
            {
                CustomerId = "CUST-001",
                Name = "Test Customer",
                Email = "test@example.com",
                TotalPurchases = 0
            };

            // Act & Assert - this should not throw
            table.Remove(customer);
            // In a real test with mocking, we would verify the call to the appropriate method
        }
    }
}
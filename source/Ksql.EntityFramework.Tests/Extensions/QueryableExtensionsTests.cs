using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Ksql.EntityFramework.Extensions;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Tests.Models;

namespace Ksql.EntityFramework.Tests.Extensions
{
    public class QueryableExtensionsTests
    {
        [Fact]
        public async Task ToListAsync_WithIKsqlTable_ShouldCallTableToListAsync()
        {
            // Arrange
            var mockTable = new Mock<IKsqlTable<TestCustomer>>();
            var expected = new List<TestCustomer> { new TestCustomer { CustomerId = "CUST-001" } };
            
            mockTable.Setup(t => t.ToListAsync()).ReturnsAsync(expected);
            
            // Make the mock implement IQueryable
            mockTable.Setup(t => t.Provider).Returns(new TestQueryProvider<TestCustomer>());
            mockTable.Setup(t => t.Expression).Returns(System.Linq.Expressions.Expression.Constant(mockTable.Object));
            mockTable.Setup(t => t.ElementType).Returns(typeof(TestCustomer));
            mockTable.Setup(t => t.GetEnumerator()).Returns(expected.GetEnumerator());

            // Act
            var result = await mockTable.Object.ToListAsync();

            // Assert
            Assert.Same(expected, result);
            mockTable.Verify(t => t.ToListAsync(), Times.Once);
        }

        [Fact]
        public async Task ToListAsync_WithIQueryable_ShouldConvertToList()
        {
            // Arrange
            var queryable = new List<TestCustomer> 
            { 
                new TestCustomer { CustomerId = "CUST-001" } 
            }.AsQueryable();

            // Act
            var result = await queryable.ToListAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal("CUST-001", result.First().CustomerId);
        }

        [Fact]
        public void OnError_WithIKsqlStream_ShouldCallStreamOnError()
        {
            // Arrange
            var mockStream = new Mock<IKsqlStream<TestOrder>>();
            mockStream.Setup(s => s.OnError(It.IsAny<ErrorAction>())).Returns(mockStream.Object);
            
            // Make the mock implement IQueryable
            mockStream.Setup(s => s.Provider).Returns(new TestQueryProvider<TestOrder>());
            mockStream.Setup(s => s.Expression).Returns(System.Linq.Expressions.Expression.Constant(mockStream.Object));
            mockStream.Setup(s => s.ElementType).Returns(typeof(TestOrder));
            mockStream.Setup(s => s.GetEnumerator()).Returns(Enumerable.Empty<TestOrder>().GetEnumerator());

            // Act
            var result = mockStream.Object.OnError(ErrorAction.Skip);

            // Assert
            Assert.Same(mockStream.Object, result);
            mockStream.Verify(s => s.OnError(ErrorAction.Skip), Times.Once);
        }

        [Fact]
        public void OnError_WithIQueryable_ShouldThrowException()
        {
            // Arrange
            var queryable = new List<TestOrder>().AsQueryable();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => queryable.OnError(ErrorAction.Skip));
        }

        // Test helper classes
        private System.Linq.Expressions.Expression Expression { get; } = System.Linq.Expressions.Expression.Constant(new List<object>());

        private class TestQueryProvider<T> : IQueryProvider
        {
            public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
            {
                return new List<T>().AsQueryable();
            }

            public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
            {
                return new List<TElement>().AsQueryable();
            }

            public object Execute(System.Linq.Expressions.Expression expression)
            {
                return null;
            }

            public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
            {
                return default;
            }
        }
    }
}
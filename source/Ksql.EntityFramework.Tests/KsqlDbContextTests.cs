using System;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Tests.Context;
using Ksql.EntityFramework.Tests.Helpers;
using Ksql.EntityFramework.Tests.Models;
using Ksql.EntityFramework.Schema;

namespace Ksql.EntityFramework.Tests
{
    public class KsqlDbContextTests
    {
        [Fact]
        public void KsqlDbContext_ShouldInitializeProperties()
        {
            // Arrange & Act
            using var context = TestHelper.CreateMockContext();

            // Assert
            Assert.NotNull(context.Orders);
            Assert.NotNull(context.Customers);
            Assert.NotNull(context.Database);
            Assert.NotNull(context.Options);
        }

        [Fact]
        public void KsqlDbContext_ShouldThrowException_WhenOptionsIsNull()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new TestKsqlDbContext(null));
        }

        [Fact]
        public async Task EnsureTopicCreatedAsync_ShouldCallDatabaseEnsureTopicCreatedAsync()
        {
            // Arrange
            var mockDatabase = new Mock<IKsqlDatabase>();
            var options = TestHelper.CreateTestOptions();

            // テスト用のコンテキスト作成（モックデータベースを使用）
            var context = new TestKsqlDbContextWithMockDb(options, mockDatabase.Object);

            // Act
            await context.EnsureTopicCreatedAsync<TestOrder>();

            // Assert
            // データベースのEnsureTopicCreatedAsyncが呼び出されたことを検証
            // ここではAny<TopicDescriptor>()を使用していますが、より厳密なマッチングを行うことも可能です
            mockDatabase.Verify(db => db.ExecuteKsqlAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task EnsureStreamCreatedAsync_ShouldCallDatabaseEnsureStreamCreatedAsync()
        {
            // Arrange
            var mockDatabase = new Mock<IKsqlDatabase>();
            var options = TestHelper.CreateTestOptions();

            // テスト用のコンテキスト作成（モックデータベースを使用）
            var context = new TestKsqlDbContextWithMockDb(options, mockDatabase.Object);

            // Act
            await context.EnsureStreamCreatedAsync<TestOrder>();

            // Assert
            // データベースのEnsureStreamCreatedAsyncが呼び出されたことを検証
            mockDatabase.Verify(db => db.ExecuteKsqlAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldProcessAllPendingChanges()
        {
            // Arrange
            var mockStream = new Mock<IKsqlStream<TestOrder>>();
            var options = TestHelper.CreateTestOptions();

            // テスト用のコンテキスト作成（モックストリームを使用）
            var context = new TestKsqlDbContextWithMockStream(options, mockStream.Object);

            var order = new TestOrder
            {
                OrderId = "TEST-001",
                CustomerId = "CUST-001",
                Amount = 100.50m,
                OrderTime = DateTimeOffset.UtcNow
            };

            // Act
            context.Orders.Add(order);
            await context.SaveChangesAsync();

            // Assert
            // ストリームのProduceAsyncが呼び出されたことを検証
            mockStream.Verify(s => s.ProduceAsync(order), Times.Once);
        }
    }

    // テスト用のコンテキストクラス（モックデータベース使用）
    public class TestKsqlDbContextWithMockDb : TestKsqlDbContext
    {
        public new IKsqlDatabase Database { get; }

        public TestKsqlDbContextWithMockDb(KsqlDbContextOptions options, IKsqlDatabase mockDatabase)
            : base(options)
        {
            Database = mockDatabase;
        }
    }

    // テスト用のコンテキストクラス（モックストリーム使用）
    public class TestKsqlDbContextWithMockStream : TestKsqlDbContext
    {
        private readonly IKsqlStream<TestOrder> _mockOrderStream;

        public new IKsqlStream<TestOrder> Orders => _mockOrderStream;

        public TestKsqlDbContextWithMockStream(KsqlDbContextOptions options, IKsqlStream<TestOrder> mockOrderStream)
            : base(options)
        {
            _mockOrderStream = mockOrderStream;
        }
    }
}
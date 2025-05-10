using System;
using Moq;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Tests.Context;
using Ksql.EntityFramework.Tests.Models;

namespace Ksql.EntityFramework.Tests.Helpers
{
    public static class TestHelper
    {
        /// <summary>
        /// テスト用のKsqlDbContextOptionsを作成します。
        /// </summary>
        public static KsqlDbContextOptions CreateTestOptions()
        {
            return new KsqlDbContextOptionsBuilder()
                .UseConnectionString("localhost:8088")
                .UseSchemaRegistry("localhost:8081")
                .UseDefaultValueFormat(ValueFormat.Json)
                .UseDeserializationErrorPolicy(ErrorPolicy.Skip)
                .UseDeadLetterQueue("test_error_topic")
                .Build();
        }

        /// <summary>
        /// 実際のサーバーに接続するテスト用のコンテキストを作成します。
        /// 注意：このメソッドは実際のKafkaとKSQL DBに接続しようとします。
        /// </summary>
        public static TestKsqlDbContext CreateTestContext()
        {
            return new TestKsqlDbContext(CreateTestOptions());
        }

        /// <summary>
        /// モック接続設定を使用するテスト用のコンテキストを作成します。
        /// 実際の接続は行わず、テスト用に適しています。
        /// </summary>
        public static TestKsqlDbContext CreateMockContext()
        {
            var options = new KsqlDbContextOptionsBuilder()
                .UseConnectionString("mock://localhost:8088")
                .UseSchemaRegistry("mock://localhost:8081")
                .UseDefaultValueFormat(ValueFormat.Json)
                .UseDeserializationErrorPolicy(ErrorPolicy.Skip)
                .UseDeadLetterQueue("test_error_topic")
                .Build();

            return new TestKsqlDbContext(options);
        }

        /// <summary>
        /// 完全にモック化されたコンテキストを作成します。
        /// 依存関係をすべてモックに置き換えます。
        /// </summary>
        public static TestKsqlDbContext CreateFullyMockedContext()
        {
            var options = CreateTestOptions();
            
            var mockDatabase = new Mock<IKsqlDatabase>();
            var mockOrders = new Mock<IKsqlStream<TestOrder>>();
            var mockCustomers = new Mock<IKsqlTable<TestCustomer>>();
            
            // モックメソッドの動作設定
            mockOrders
                .Setup(m => m.ProduceAsync(It.IsAny<TestOrder>()))
                .ReturnsAsync(1L);
                
            mockCustomers
                .Setup(m => m.FindAsync(It.IsAny<object>()))
                .ReturnsAsync((TestCustomer)null);
            
            return new TestMockedKsqlDbContext(options, mockDatabase.Object, mockOrders.Object, mockCustomers.Object);
        }
    }

    /// <summary>
    /// 完全にモック化されたテスト用のコンテキストクラス
    /// </summary>
    public class TestMockedKsqlDbContext : TestKsqlDbContext
    {
        public new IKsqlDatabase Database { get; }
        public new IKsqlStream<TestOrder> Orders { get; }
        public new IKsqlTable<TestCustomer> Customers { get; }

        public TestMockedKsqlDbContext(
            KsqlDbContextOptions options,
            IKsqlDatabase database,
            IKsqlStream<TestOrder> orders,
            IKsqlTable<TestCustomer> customers) 
            : base(options)
        {
            Database = database;
            Orders = orders;
            Customers = customers;
        }
    }
}
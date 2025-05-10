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
        /// �e�X�g�p��KsqlDbContextOptions���쐬���܂��B
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
        /// ���ۂ̃T�[�o�[�ɐڑ�����e�X�g�p�̃R���e�L�X�g���쐬���܂��B
        /// ���ӁF���̃��\�b�h�͎��ۂ�Kafka��KSQL DB�ɐڑ����悤�Ƃ��܂��B
        /// </summary>
        public static TestKsqlDbContext CreateTestContext()
        {
            return new TestKsqlDbContext(CreateTestOptions());
        }

        /// <summary>
        /// ���b�N�ڑ��ݒ���g�p����e�X�g�p�̃R���e�L�X�g���쐬���܂��B
        /// ���ۂ̐ڑ��͍s�킸�A�e�X�g�p�ɓK���Ă��܂��B
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
        /// ���S�Ƀ��b�N�����ꂽ�R���e�L�X�g���쐬���܂��B
        /// �ˑ��֌W�����ׂă��b�N�ɒu�������܂��B
        /// </summary>
        public static TestKsqlDbContext CreateFullyMockedContext()
        {
            var options = CreateTestOptions();
            
            var mockDatabase = new Mock<IKsqlDatabase>();
            var mockOrders = new Mock<IKsqlStream<TestOrder>>();
            var mockCustomers = new Mock<IKsqlTable<TestCustomer>>();
            
            // ���b�N���\�b�h�̓���ݒ�
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
    /// ���S�Ƀ��b�N�����ꂽ�e�X�g�p�̃R���e�L�X�g�N���X
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
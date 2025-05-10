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

            // �e�X�g�p�̃R���e�L�X�g�쐬�i���b�N�f�[�^�x�[�X���g�p�j
            var context = new TestKsqlDbContextWithMockDb(options, mockDatabase.Object);

            // Act
            await context.EnsureTopicCreatedAsync<TestOrder>();

            // Assert
            // �f�[�^�x�[�X��EnsureTopicCreatedAsync���Ăяo���ꂽ���Ƃ�����
            // �����ł�Any<TopicDescriptor>()���g�p���Ă��܂����A��茵���ȃ}�b�`���O���s�����Ƃ��\�ł�
            mockDatabase.Verify(db => db.ExecuteKsqlAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task EnsureStreamCreatedAsync_ShouldCallDatabaseEnsureStreamCreatedAsync()
        {
            // Arrange
            var mockDatabase = new Mock<IKsqlDatabase>();
            var options = TestHelper.CreateTestOptions();

            // �e�X�g�p�̃R���e�L�X�g�쐬�i���b�N�f�[�^�x�[�X���g�p�j
            var context = new TestKsqlDbContextWithMockDb(options, mockDatabase.Object);

            // Act
            await context.EnsureStreamCreatedAsync<TestOrder>();

            // Assert
            // �f�[�^�x�[�X��EnsureStreamCreatedAsync���Ăяo���ꂽ���Ƃ�����
            mockDatabase.Verify(db => db.ExecuteKsqlAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task SaveChangesAsync_ShouldProcessAllPendingChanges()
        {
            // Arrange
            var mockStream = new Mock<IKsqlStream<TestOrder>>();
            var options = TestHelper.CreateTestOptions();

            // �e�X�g�p�̃R���e�L�X�g�쐬�i���b�N�X�g���[�����g�p�j
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
            // �X�g���[����ProduceAsync���Ăяo���ꂽ���Ƃ�����
            mockStream.Verify(s => s.ProduceAsync(order), Times.Once);
        }
    }

    // �e�X�g�p�̃R���e�L�X�g�N���X�i���b�N�f�[�^�x�[�X�g�p�j
    public class TestKsqlDbContextWithMockDb : TestKsqlDbContext
    {
        public new IKsqlDatabase Database { get; }

        public TestKsqlDbContextWithMockDb(KsqlDbContextOptions options, IKsqlDatabase mockDatabase)
            : base(options)
        {
            Database = mockDatabase;
        }
    }

    // �e�X�g�p�̃R���e�L�X�g�N���X�i���b�N�X�g���[���g�p�j
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
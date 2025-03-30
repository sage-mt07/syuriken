using System;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Tests.Context;

namespace Ksql.EntityFramework.Tests.Helpers
{
    public static class TestHelper
    {
        public static KsqlDbContextOptions CreateTestOptions()
        {
            return new KsqlDbContextOptionsBuilder()
                .UseConnectionString("localhost:8088")
                .UseSchemaRegistry("localhost:8081")
                .UseDefaultValueFormat(Ksql.EntityFramework.Models.ValueFormat.Json)
                .UseDeserializationErrorPolicy(Ksql.EntityFramework.Models.ErrorPolicy.Skip)
                .UseDeadLetterQueue("test_error_topic")
                .Build();
        }

        public static TestKsqlDbContext CreateTestContext()
        {
            return new TestKsqlDbContext(CreateTestOptions());
        }

        public static TestKsqlDbContext CreateMockContext()
        {
            var options = new KsqlDbContextOptionsBuilder()
                .UseConnectionString("mock://localhost:8088")
                .UseSchemaRegistry("mock://localhost:8081")
                .UseDefaultValueFormat(Ksql.EntityFramework.Models.ValueFormat.Json)
                .UseDeserializationErrorPolicy(Ksql.EntityFramework.Models.ErrorPolicy.Skip)
                .UseDeadLetterQueue("test_error_topic")
                .Build();

            return new TestKsqlDbContext(options);
        }
    }
}
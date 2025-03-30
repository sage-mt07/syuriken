using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Tests.Models;

namespace Ksql.EntityFramework.Tests.Context
{
    public class TestKsqlDbContext : KsqlDbContext
    {
        public IKsqlStream<TestOrder> Orders { get; set; } = null!;
        public IKsqlTable<TestCustomer> Customers { get; set; } = null!;

        public TestKsqlDbContext(KsqlDbContextOptions options) : base(options)
        {
        }
    }
}
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Examples.Models;
using Ksql.EntityFramework.Interfaces;

namespace Ksql.EntityFramework.Examples
{
    /// <summary>
    /// Example context for working with orders and customers.
    /// </summary>
    public class OrderContext : KsqlDbContext
    {
        /// <summary>
        /// Gets the stream of orders.
        /// </summary>
        public IKsqlStream<Order> Orders { get; set; } = null!;

        /// <summary>
        /// Gets the table of customers.
        /// </summary>
        public IKsqlTable<Customer> Customers { get; set; } = null!;

        /// <summary>
        /// Gets the table of order summaries.
        /// </summary>
        public IKsqlTable<OrderSummary> OrderSummaries => CreateTable<OrderSummary>("order_summaries_table",
            builder => builder.FromStream(Orders));

        /// <summary>
        /// Gets the table of customer order ranges.
        /// </summary>
        public IKsqlTable<CustomerOrderRange> CustomerOrderRanges => CreateTable<CustomerOrderRange>("customer_order_ranges_table",
            builder => builder.FromStream(Orders));

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderContext"/> class.
        /// </summary>
        public OrderContext() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderContext"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options for the context.</param>
        public OrderContext(KsqlDbContextOptions options) : base(options)
        {
        }
    }
}

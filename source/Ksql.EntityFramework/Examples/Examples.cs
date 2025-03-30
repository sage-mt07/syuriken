using System;
using System.Linq;
using System.Threading.Tasks;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Examples.Models;
using Ksql.EntityFramework.Extensions;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework.Examples
{
    /// <summary>
    /// Examples of using the KSQL Entity Framework.
    /// </summary>
    public static class Examples
    {
        /// <summary>
        /// Example of setting up a context and creating topics, streams, and tables.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task SetupExampleAsync()
        {
            // Create options for the context
            var options = new KsqlDbContextOptionsBuilder()
                .UseConnectionString("http://localhost:8088")
                .UseSchemaRegistry("http://localhost:8081")
                .UseDefaultValueFormat(ValueFormat.Avro)
                .UseDeserializationErrorPolicy(ErrorPolicy.Skip)
                .UseDeadLetterQueue("error_topic", (data, error) => new DeadLetterMessage
                {
                    OriginalData = data,
                    ErrorMessage = error.Message,
                    Timestamp = DateTimeOffset.UtcNow
                })
                .Build();

            // Create the context
            using var context = new OrderContext(options);

            // Ensure topics, streams, and tables exist
            await context.EnsureTopicCreatedAsync<Order>();
            await context.EnsureTopicCreatedAsync<Customer>();
            await context.EnsureStreamCreatedAsync<Order>();
            await context.EnsureStreamCreatedAsync<Customer>();
            await context.EnsureTableCreatedAsync(context.OrderSummaries);
        }

        /// <summary>
        /// Example of producing data to streams and tables.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task ProduceDataExampleAsync()
        {
            // Create the context
            using var context = new OrderContext();

            // Produce orders to the stream
            var order1 = new Order
            {
                OrderId = "ORD-001",
                CustomerId = "CUST-001",
                Amount = 100.50m,
                OrderTime = DateTimeOffset.UtcNow
            };

            var order2 = new Order
            {
                OrderId = "ORD-002",
                CustomerId = "CUST-001",
                Amount = 200.75m,
                OrderTime = DateTimeOffset.UtcNow.AddMinutes(5)
            };

            var order3 = new Order
            {
                OrderId = "ORD-003",
                CustomerId = "CUST-002",
                Amount = 150.25m,
                OrderTime = DateTimeOffset.UtcNow.AddMinutes(10)
            };

            // Use the low-level API to produce single records
            await context.Orders.ProduceAsync(order1);
            await context.Orders.ProduceAsync("CUST-001", order2);

            // Or use the Entity Framework-like API
            context.Orders.Add(order3);

            // Produce a customer to the table
            var customer = new Customer
            {
                CustomerId = "CUST-001",
                Name = "John Doe",
                Email = "john.doe@example.com",
                Region = "US-WEST",
                TotalPurchases = 301.25m
            };

            context.Customers.Add(customer);

            // Save all changes in one batch
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Example of querying data from streams and tables.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task QueryDataExampleAsync()
        {
            // Create the context
            using var context = new OrderContext();

            // Query high value orders from the stream
            var highValueOrders = context.Orders
                .Where(o => o.Amount > 1000)
                .Select(o => new { o.OrderId, o.CustomerId, o.Amount });

            // Query customer data from the table
            var customer = await context.Customers.FindAsync("CUST-001");

            // Query high value customers from the table
            var highValueCustomers = await context.Customers
                .Where(c => c.TotalPurchases > 10000)
                .OrderByDescending(c => c.TotalPurchases)
                .ToListAsync();

            // Window operations
            var hourlyStats = context.Orders
                .Window(TumblingWindow.Of(TimeSpan.FromHours(1)))
                .GroupBy(o => o.CustomerId);

            // Subscribe to stream changes
            await SubscribeToOrdersExampleAsync(context);
        }

        /// <summary>
        /// Example of subscribing to a stream of orders.
        /// </summary>
        /// <param name="context">The order context.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task SubscribeToOrdersExampleAsync(OrderContext context)
        {
            // Subscribe to all orders
            await foreach (var order in context.Orders.SubscribeAsync())
            {
                Console.WriteLine($"Received order: {order.OrderId}, Amount: {order.Amount}");
            }

            // Subscribe to only high value orders with error handling
            await foreach (var order in context.Orders
                .Where(o => o.Amount > 1000)
                .OnError(ErrorAction.Skip)
                .SubscribeAsync())
            {
                Console.WriteLine($"Received high-value order: {order.OrderId}, Amount: {order.Amount}");
            }

            // Observe changes to the customers table
            await foreach (var change in context.Customers.ObserveChangesAsync())
            {
                if (change.ChangeType == ChangeType.Insert)
                {
                    Console.WriteLine($"New customer: {change.Entity.Name}");
                }
                else if (change.ChangeType == ChangeType.Update)
                {
                    Console.WriteLine($"Updated customer: {change.Entity.Name}");
                }
                else if (change.ChangeType == ChangeType.Delete)
                {
                    Console.WriteLine($"Deleted customer: {change.Entity.Name}");
                }
            }
        }

        /// <summary>
        /// Example of using transactions.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task TransactionExampleAsync()
        {
            // Create the context
            using var context = new OrderContext();

            // Start a transaction
            using var transaction = await context.BeginTransactionAsync();

            try
            {
                // Add orders to the stream
                context.Orders.Add(new Order
                {
                    OrderId = "ORD-004",
                    CustomerId = "CUST-002",
                    Amount = 300.00m,
                    OrderTime = DateTimeOffset.UtcNow
                });

                context.Orders.Add(new Order
                {
                    OrderId = "ORD-005",
                    CustomerId = "CUST-002",
                    Amount = 400.00m,
                    OrderTime = DateTimeOffset.UtcNow.AddMinutes(1)
                });

                // Update the customer's total purchases
                var customer = await context.Customers.FindAsync("CUST-002");
                if (customer != null)
                {
                    customer.TotalPurchases += 700.00m;
                    context.Customers.Add(customer);
                }

                // Save changes and commit the transaction
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // Abort the transaction if an error occurs
                await transaction.AbortAsync();
                throw;
            }
        }
    }
}

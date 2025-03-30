using System;
using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Examples.Models
{
    /// <summary>
    /// Represents a summary of orders for a customer.
    /// </summary>
    public class OrderSummary
    {
        /// <summary>
        /// Gets or sets the ID of the customer.
        /// </summary>
        [Key]
        public string CustomerId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the customer.
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the count of orders placed by the customer.
        /// </summary>
        public int OrderCount { get; set; }

        /// <summary>
        /// Gets or sets the total amount of orders placed by the customer.
        /// </summary>
        [DecimalPrecision(18, 2)]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the time of the first order placed by the customer.
        /// </summary>
        public DateTimeOffset FirstOrderTime { get; set; }

        /// <summary>
        /// Gets or sets the time of the latest order placed by the customer.
        /// </summary>
        public DateTimeOffset LatestOrderTime { get; set; }
    }
}

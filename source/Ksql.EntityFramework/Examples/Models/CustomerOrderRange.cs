using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Examples.Models;

/// <summary>
/// Represents a range of orders for a customer with loyalty information.
/// </summary>
public class CustomerOrderRange
{
    /// <summary>
    /// Gets or sets the ID of the customer.
    /// </summary>
    [Key]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time of the first order placed by the customer.
    /// </summary>
    public DateTimeOffset FirstOrderTime { get; set; }

    /// <summary>
    /// Gets or sets the time of the latest order placed by the customer.
    /// </summary>
    public DateTimeOffset LatestOrderTime { get; set; }

    /// <summary>
    /// Gets or sets the count of orders placed by the customer.
    /// </summary>
    public int OrderCount { get; set; }

    /// <summary>
    /// Gets or sets the total amount spent by the customer.
    /// </summary>
    [DecimalPrecision(18, 2)]
    public decimal TotalSpent { get; set; }

    /// <summary>
    /// Gets or sets the number of days between the first and latest order (loyalty days).
    /// </summary>
    public int LoyaltyDays { get; set; }
}

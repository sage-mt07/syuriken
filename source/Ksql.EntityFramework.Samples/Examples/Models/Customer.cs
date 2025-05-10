using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Examples.Models;

/// <summary>
/// Represents a customer in the system.
/// </summary>
[Topic("customers")]
public class Customer
{
    /// <summary>
    /// Gets or sets the ID of the customer.
    /// </summary>
    [Key]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the customer.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the email address of the customer.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the region of the customer.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total purchases made by the customer.
    /// </summary>
    [DecimalPrecision(18, 2)]
    public decimal TotalPurchases { get; set; }
}

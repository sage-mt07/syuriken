using System;
using Ksql.EntityFramework.Attributes;

namespace Ksql.EntityFramework.Tests.Models
{
    [Topic("test_customers")]
    public class TestCustomer
    {
        [Key]
        public string CustomerId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [DecimalPrecision(18, 2)]
        public decimal TotalPurchases { get; set; }
    }
}
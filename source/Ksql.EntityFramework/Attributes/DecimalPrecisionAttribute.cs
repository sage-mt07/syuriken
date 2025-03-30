using System;

namespace Ksql.EntityFramework.Attributes
{
    /// <summary>
    /// Specifies the precision and scale for a decimal property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DecimalPrecisionAttribute : Attribute
    {
        /// <summary>
        /// Gets the precision (total number of digits).
        /// </summary>
        public int Precision { get; }

        /// <summary>
        /// Gets the scale (number of decimal places).
        /// </summary>
        public int Scale { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DecimalPrecisionAttribute"/> class.
        /// </summary>
        /// <param name="precision">The precision (total number of digits).</param>
        /// <param name="scale">The scale (number of decimal places).</param>
        public DecimalPrecisionAttribute(int precision, int scale)
        {
            if (precision <= 0) throw new ArgumentOutOfRangeException(nameof(precision), "Precision must be greater than zero.");
            if (scale < 0 || scale > precision) throw new ArgumentOutOfRangeException(nameof(scale), "Scale must be between 0 and precision.");

            Precision = precision;
            Scale = scale;
        }
    }
}

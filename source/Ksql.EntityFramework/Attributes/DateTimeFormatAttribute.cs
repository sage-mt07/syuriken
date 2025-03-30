using System;

namespace Ksql.EntityFramework.Attributes
{
    /// <summary>
    /// Specifies the format and locale for a DateTime property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DateTimeFormatAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the format string for the DateTime property.
        /// </summary>
        public string? Format { get; set; }

        /// <summary>
        /// Gets or sets the locale for the DateTime property.
        /// </summary>
        public string? Locale { get; set; }
    }
}

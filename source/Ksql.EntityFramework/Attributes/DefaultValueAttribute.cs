using System;

namespace Ksql.EntityFramework.Attributes
{
    /// <summary>
    /// Specifies a default value for a property if not otherwise specified.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : Attribute
    {
        /// <summary>
        /// Gets the default value for the property.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultValueAttribute"/> class.
        /// </summary>
        /// <param name="value">The default value for the property.</param>
        public DefaultValueAttribute(object? value)
        {
            Value = value;
        }
    }
}

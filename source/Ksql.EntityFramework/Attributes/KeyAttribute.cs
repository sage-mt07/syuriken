namespace Ksql.EntityFramework.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the order of the key in a composite key.
    /// Lower values have higher priority. Default is 0.
    /// </summary>
    public int Order { get; set; } = 0;
}
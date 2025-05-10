namespace Ksql.EntityFramework.Attributes;

/// <summary>
/// Specifies that a property represents a primary key for a Kafka topic.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute
{
}

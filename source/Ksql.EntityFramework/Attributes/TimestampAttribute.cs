namespace Ksql.EntityFramework.Attributes;

/// <summary>
/// Defines the types of timestamps in Kafka.
/// </summary>
public enum TimestampType
{
    /// <summary>
    /// Event time when the event occurred.
    /// </summary>
    EventTime,

    /// <summary>
    /// Processing time when the event is processed.
    /// </summary>
    ProcessingTime
}

/// <summary>
/// Specifies that a property represents a timestamp for Kafka events.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class TimestampAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the format string for the timestamp.
    /// </summary>
    public string? Format { get; set; }

    /// <summary>
    /// Gets or sets the type of timestamp.
    /// </summary>
    public TimestampType Type { get; set; } = TimestampType.EventTime;
}

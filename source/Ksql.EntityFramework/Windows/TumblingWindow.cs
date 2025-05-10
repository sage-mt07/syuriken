namespace Ksql.EntityFramework.Windows;

/// <summary>
/// Represents a tumbling window specification in KSQL.
/// </summary>
public class TumblingWindow : WindowSpecification
{
    /// <summary>
    /// Gets the size of the window.
    /// </summary>
    public TimeSpan Size { get; }

    /// <summary>
    /// Gets the type of window.
    /// </summary>
    public override WindowType WindowType => WindowType.Tumbling;

    /// <summary>
    /// Initializes a new instance of the <see cref="TumblingWindow"/> class with the specified size.
    /// </summary>
    /// <param name="size">The size of the window.</param>
    public TumblingWindow(TimeSpan size)
    {
        Size = size;
    }

    /// <summary>
    /// Creates a tumbling window with the specified size.
    /// </summary>
    /// <param name="size">The size of the window.</param>
    /// <returns>A tumbling window specification.</returns>
    public static TumblingWindow Of(TimeSpan size)
    {
        return new TumblingWindow(size);
    }

    /// <summary>
    /// Gets a string representation of the window specification in KSQL syntax.
    /// </summary>
    /// <returns>A string representing the window specification.</returns>
    public override string ToKsqlString()
    {
        // Convert the TimeSpan to a KSQL time unit
        string timeUnit;
        long value;

        if (Size.TotalMilliseconds < 1000)
        {
            timeUnit = "MILLISECONDS";
            value = (long)Size.TotalMilliseconds;
        }
        else if (Size.TotalSeconds < 60)
        {
            timeUnit = "SECONDS";
            value = (long)Size.TotalSeconds;
        }
        else if (Size.TotalMinutes < 60)
        {
            timeUnit = "MINUTES";
            value = (long)Size.TotalMinutes;
        }
        else if (Size.TotalHours < 24)
        {
            timeUnit = "HOURS";
            value = (long)Size.TotalHours;
        }
        else
        {
            timeUnit = "DAYS";
            value = (long)Size.TotalDays;
        }

        return $"TUMBLING (SIZE {value} {timeUnit})";
    }
}

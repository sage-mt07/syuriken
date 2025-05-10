namespace Ksql.EntityFramework.Models;

/// <summary>
/// Specifies the action to take when an error occurs during stream processing.
/// </summary>
public enum ErrorAction
{
    /// <summary>
    /// Stop processing when an error occurs.
    /// </summary>
    Stop,

    /// <summary>
    /// Skip the record that caused the error and continue processing.
    /// </summary>
    Skip,

    /// <summary>
    /// Log the error and continue processing.
    /// </summary>
    LogAndContinue,

    /// <summary>
    /// Send the error record to a dead letter queue and continue processing.
    /// </summary>
    DeadLetterQueue
}

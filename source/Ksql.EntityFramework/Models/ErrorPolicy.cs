namespace Ksql.EntityFramework.Models
{
    /// <summary>
    /// Specifies the policy for handling errors during deserialization or processing.
    /// </summary>
    public enum ErrorPolicy
    {
        /// <summary>
        /// Abort processing when an error occurs.
        /// </summary>
        Abort,

        /// <summary>
        /// Skip the record that caused the error and continue processing.
        /// </summary>
        Skip,

        /// <summary>
        /// Retry the operation that caused the error.
        /// </summary>
        Retry,

        /// <summary>
        /// Send the error record to a dead letter queue and continue processing.
        /// </summary>
        DeadLetterQueue
    }
}

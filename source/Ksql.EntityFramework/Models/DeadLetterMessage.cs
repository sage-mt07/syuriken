namespace Ksql.EntityFramework.Models
{
    /// <summary>
    /// Represents a message that was sent to a dead letter queue due to an error.
    /// </summary>
    public class DeadLetterMessage
    {
        /// <summary>
        /// Gets or sets the original data that caused the error.
        /// </summary>
        public byte[]? OriginalData { get; set; }

        /// <summary>
        /// Gets or sets the error message that describes why the message was sent to the dead letter queue.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the message was sent to the dead letter queue.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; }

        /// <summary>
        /// Gets or sets the topic from which the original message came.
        /// </summary>
        public string? SourceTopic { get; set; }

        /// <summary>
        /// Gets or sets additional context information about the error.
        /// </summary>
        public string? ErrorContext { get; set; }
    }
}

namespace Ksql.EntityFramework.Models
{
    /// <summary>
    /// Represents a notification for a change to a record in a stream or table.
    /// </summary>
    /// <typeparam name="T">The type of entity that changed.</typeparam>
    public class ChangeNotification<T> where T : class
    {
        /// <summary>
        /// Gets the type of change that occurred.
        /// </summary>
        public ChangeType ChangeType { get; }

        /// <summary>
        /// Gets the entity that was changed.
        /// </summary>
        public T Entity { get; }

        /// <summary>
        /// Gets the key of the entity that was changed.
        /// </summary>
        public object Key { get; }

        /// <summary>
        /// Gets the previous state of the entity, if available.
        /// </summary>
        public T? PreviousEntity { get; }

        /// <summary>
        /// Gets the timestamp of the change.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ChangeNotification{T}"/> class.
        /// </summary>
        /// <param name="changeType">The type of change.</param>
        /// <param name="entity">The changed entity.</param>
        /// <param name="key">The key of the entity.</param>
        /// <param name="previousEntity">The previous state of the entity, if available.</param>
        /// <param name="timestamp">The timestamp of the change.</param>
        public ChangeNotification(ChangeType changeType, T entity, object key, T? previousEntity, DateTimeOffset timestamp)
        {
            ChangeType = changeType;
            Entity = entity;
            Key = key;
            PreviousEntity = previousEntity;
            Timestamp = timestamp;
        }
    }
}

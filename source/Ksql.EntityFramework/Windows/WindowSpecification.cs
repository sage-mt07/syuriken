namespace Ksql.EntityFramework.Windows
{
    /// <summary>
    /// Base class for window specifications in KSQL.
    /// </summary>
    public abstract class WindowSpecification
    {
        /// <summary>
        /// Gets the type of window.
        /// </summary>
        public abstract WindowType WindowType { get; }

        /// <summary>
        /// Gets a string representation of the window specification in KSQL syntax.
        /// </summary>
        /// <returns>A string representing the window specification.</returns>
        public abstract string ToKsqlString();
    }

    /// <summary>
    /// Specifies the types of windows available in KSQL.
    /// </summary>
    public enum WindowType
    {
        /// <summary>
        /// A tumbling window groups records into fixed-size, non-overlapping windows based on time.
        /// </summary>
        Tumbling,

        /// <summary>
        /// A hopping window groups records into fixed-size, possibly overlapping windows based on time.
        /// </summary>
        Hopping,

        /// <summary>
        /// A session window groups records into windows based on activity, with gaps of inactivity separating windows.
        /// </summary>
        Session
    }
}

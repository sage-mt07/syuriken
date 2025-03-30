using Ksql.EntityFramework.Windows;
using System.Linq;

namespace Ksql.EntityFramework.Interfaces
{
    /// <summary>
    /// Represents a windowed KSQL stream that can be queried and processed with window operations.
    /// </summary>
    /// <typeparam name="T">The type of entity in the stream.</typeparam>
    public interface IWindowedKsqlStream<T> : IQueryable<T> where T : class
    {
        /// <summary>
        /// Gets the window specification for this windowed stream.
        /// </summary>
        WindowSpecification WindowSpecification { get; }
    }
}

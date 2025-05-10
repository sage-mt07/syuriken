using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework.Interfaces;

/// <summary>
/// Represents a KSQL stream that can be queried and subscribed to.
/// </summary>
/// <typeparam name="T">The type of entity in the stream.</typeparam>
public interface IKsqlStream<T> : IQueryable<T> where T : class
{
    /// <summary>
    /// Produces a single entity to the stream with an auto-generated key.
    /// </summary>
    /// <param name="entity">The entity to produce.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating the offset of the produced record.</returns>
    Task<long> ProduceAsync(T entity);

    /// <summary>
    /// Produces a single entity to the stream with the specified key.
    /// </summary>
    /// <param name="key">The key for the record.</param>
    /// <param name="entity">The entity to produce.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating the offset of the produced record.</returns>
    Task<long> ProduceAsync(string key, T entity);

    /// <summary>
    /// Produces multiple entities to the stream in a batch.
    /// </summary>
    /// <param name="entities">The entities to produce.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ProduceBatchAsync(IEnumerable<T> entities);

    /// <summary>
    /// Subscribes to the stream and receives entities as they are produced.
    /// </summary>
    /// <returns>An asynchronous enumerable of entities.</returns>
    IEnumerable<T> Subscribe();

    /// <summary>
    /// Configures a watermark for this stream based on the timestamp property.
    /// </summary>
    /// <param name="timestampSelector">A function to select the timestamp property.</param>
    /// <param name="maxOutOfOrderness">The maximum out-of-orderness to allow.</param>
    /// <returns>The stream with watermark configured.</returns>
    IKsqlStream<T> WithWatermark<TTimestamp>(System.Linq.Expressions.Expression<Func<T, TTimestamp>> timestampSelector, TimeSpan maxOutOfOrderness);

    /// <summary>
    /// Configures error handling for this stream.
    /// </summary>
    /// <param name="errorAction">The action to take when an error occurs.</param>
    /// <returns>The stream with error handling configured.</returns>
    IKsqlStream<T> OnError(ErrorAction errorAction);

    /// <summary>
    /// Creates a windowed stream using a tumbling window.
    /// </summary>
    /// <param name="window">The window specification.</param>
    /// <returns>A windowed stream.</returns>
    IWindowedKsqlStream<T> Window(WindowSpecification window);



    /// <summary>
    /// Adds a stream entity to be saved when SaveChanges is called.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    void Add(T entity);
}

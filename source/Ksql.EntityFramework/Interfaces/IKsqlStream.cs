using System.Linq.Expressions;
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
    /// <param name="onNext">The action to invoke when a new entity is produced.</param>
    /// <param name="onError">The action to invoke when an error occurs.</param>
    /// <param name="onCompleted">The action to invoke when the subscription is completed.</param>
    /// <param name="cancellationToken">A token to cancel the subscription.</param>
    /// <returns>A subscription that can be used to cancel the subscription.</returns>
    IDisposable Subscribe(
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Joins this stream with another stream within a specified window.
    /// </summary>
    /// <typeparam name="TRight">The type of entity in the right stream.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="rightStream">The right stream to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this stream's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right stream's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <param name="window">The window specification for the join.</param>
    /// <returns>A stream containing the joined elements.</returns>
    IKsqlStream<TResult> Join<TRight, TKey, TResult>(
        IKsqlStream<TRight> rightStream,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector,
        WindowSpecification window)
        where TRight : class
        where TResult : class;

    /// <summary>
    /// Joins this stream with a table.
    /// </summary>
    /// <typeparam name="TRight">The type of entity in the table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="table">The table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this stream's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A stream containing the joined elements.</returns>
    IKsqlStream<TResult> Join<TRight, TKey, TResult>(
        IKsqlTable<TRight> table,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector)
        where TRight : class
        where TResult : class;

    /// <summary>
    /// Left joins this stream with a table.
    /// </summary>
    /// <typeparam name="TRight">The type of entity in the table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="table">The table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this stream's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A stream containing the joined elements.</returns>
    IKsqlStream<TResult> LeftJoin<TRight, TKey, TResult>(
        IKsqlTable<TRight> table,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector)
        where TRight : class
        where TResult : class;
}

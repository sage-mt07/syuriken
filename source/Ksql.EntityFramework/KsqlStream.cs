using System.Collections;
using System.Linq.Expressions;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Schema;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework;

/// <summary>
/// Implementation of a KSQL stream.
/// </summary>
/// <typeparam name="T">The type of entity in the stream.</typeparam>
public class KsqlStream<T> : IKsqlStream<T> where T : class
{
    private readonly KsqlDbContext _context;
    private readonly SchemaManager _schemaManager;
    private readonly List<T> _pendingAdds = new List<T>();
    private ErrorAction _errorAction = ErrorAction.Stop;

    /// <summary>
    /// Gets the name of the stream.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the provider.
    /// </summary>
    public Type ElementType => typeof(T);

    /// <summary>
    /// Gets the query expression.
    /// </summary>
    public Expression Expression => Expression.Constant(this);

    /// <summary>
    /// Gets the query provider.
    /// </summary>
    public IQueryProvider Provider => new KsqlQueryProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlStream{T}"/> class.
    /// </summary>
    /// <param name="name">The name of the stream.</param>
    /// <param name="context">The database context.</param>
    /// <param name="schemaManager">The schema manager.</param>
    public KsqlStream(string name, KsqlDbContext context, SchemaManager schemaManager)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
    }

    private Kafka.KafkaProducer<string, T> GetProducer()
    {
        return new Kafka.KafkaProducer<string, T>(Name, _context.Options);
    }

    private Kafka.KafkaConsumer<string, T> GetConsumer(string groupId = null)
    {
        return new Kafka.KafkaConsumer<string, T>(Name, _context.Options, groupId);
    }

    /// <summary>
    /// Produces a single entity to the stream with an auto-generated key.
    /// </summary>
    /// <param name="entity">The entity to produce.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating the offset of the produced record.</returns>
    public async Task<long> ProduceAsync(T entity)
    {
        // Generate a key if the entity has a key attribute
        string key = GetEntityKey(entity) ?? Guid.NewGuid().ToString();

        using var producer = GetProducer();
        var result = await producer.ProduceAsync(key, entity);
        return result.Offset.Value;
    }

    /// <summary>
    /// Produces a single entity to the stream with the specified key.
    /// </summary>
    /// <param name="key">The key for the record.</param>
    /// <param name="entity">The entity to produce.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating the offset of the produced record.</returns>
    public async Task<long> ProduceAsync(string key, T entity)
    {
        using var producer = GetProducer();
        var result = await producer.ProduceAsync(key, entity);
        return result.Offset.Value;
    }

    /// <summary>
    /// Produces multiple entities to the stream in a batch.
    /// </summary>
    /// <param name="entities">The entities to produce.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ProduceBatchAsync(IEnumerable<T> entities)
    {
        var keyValuePairs = new List<KeyValuePair<string, T>>();

        foreach (var entity in entities)
        {
            string key = GetEntityKey(entity) ?? Guid.NewGuid().ToString();
            keyValuePairs.Add(new KeyValuePair<string, T>(key, entity));
        }

        using var producer = GetProducer();
        await producer.ProduceBatchAsync(keyValuePairs);
    }

    private string GetEntityKey(T entity)
    {
        // Find properties with [Key] attribute and get their values
        var keyProperties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(Attributes.KeyAttribute), true).Any());

        foreach (var prop in keyProperties)
        {
            var value = prop.GetValue(entity)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds a stream entity to be saved when SaveChanges is called.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    public void Add(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _pendingAdds.Add(entity);
        _context.AddToPendingChanges(entity);
    }

    /// <summary>
    /// Subscribes to the stream and receives entities as they are produced.
    /// </summary>
    /// <returns>An asynchronous enumerable of entities.</returns>
    public IEnumerable<T> Subscribe()
    {
        var consumer = GetConsumer();

        try
        {
            foreach (var entity in consumer.Consume())
            {
                yield return entity;
            }
        }
        finally
        {
            consumer.Dispose();
        }
    }

    /// <summary>
    /// Configures a watermark for this stream based on the timestamp property.
    /// </summary>
    /// <param name="timestampSelector">A function to select the timestamp property.</param>
    /// <param name="maxOutOfOrderness">The maximum out-of-orderness to allow.</param>
    /// <returns>The stream with watermark configured.</returns>
    public IKsqlStream<T> WithWatermark<TTimestamp>(Expression<Func<T, TTimestamp>> timestampSelector, TimeSpan maxOutOfOrderness)
    {
        // This is a placeholder implementation for configuring a watermark
        // In a real implementation, this would configure a watermark on the stream
        return this;
    }

    /// <summary>
    /// Configures error handling for this stream.
    /// </summary>
    /// <param name="errorAction">The action to take when an error occurs.</param>
    /// <returns>The stream with error handling configured.</returns>
    public IKsqlStream<T> OnError(ErrorAction errorAction)
    {
        _errorAction = errorAction;
        return this;
    }

    /// <summary>
    /// Creates a windowed stream using a tumbling window.
    /// </summary>
    /// <param name="window">The window specification.</param>
    /// <returns>A windowed stream.</returns>
    public IWindowedKsqlStream<T> Window(WindowSpecification window)
    {
        return new WindowedKsqlStream<T>(this, window);
    }


    /// <summary>
    /// Gets an enumerator for the elements in the stream.
    /// </summary>
    /// <returns>An enumerator for the elements in the stream.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        // This is a placeholder implementation for enumerating a stream
        // In a real implementation, this would execute a query against the stream
        return Enumerable.Empty<T>().GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator for the elements in the stream.
    /// </summary>
    /// <returns>An enumerator for the elements in the stream.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    /// <summary>
    /// Subscribes to the stream and receives entities as they are produced.
    /// </summary>
    /// <param name="onNext">The action to invoke when a new entity is produced.</param>
    /// <param name="onError">The action to invoke when an error occurs.</param>
    /// <param name="onCompleted">The action to invoke when the subscription is completed.</param>
    /// <param name="cancellationToken">A token to cancel the subscription.</param>
    /// <returns>A subscription that can be used to cancel the subscription.</returns>
    public IDisposable Subscribe(
        Action<T> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null,
        CancellationToken cancellationToken = default)
    {
        if (onNext == null) throw new ArgumentNullException(nameof(onNext));

        // Create a subscription that processes entities as they are produced
        var subscription = new KsqlSubscription<T>(
            async () =>
            {
                try
                {
                     foreach (var entity in Subscribe())
                    {
                        try
                        {
                            onNext(entity);
                        }
                        catch (Exception ex)
                        {
                            onError?.Invoke(ex);
                            if (_errorAction == Models.ErrorAction.Stop)
                            {
                                break;
                            }
                        }
                    }

                    onCompleted?.Invoke();
                }
                catch (OperationCanceledException)
                {
                    // Subscription was canceled - this is expected
                    onCompleted?.Invoke();
                }
                catch (Exception ex)
                {
                    onError?.Invoke(ex);
                }
            });

        // Start the subscription
        subscription.Start();

        return subscription;
    }
    
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
    public IKsqlStream<TResult> Join<TRight, TKey, TResult>(
        IKsqlStream<TRight> rightStream,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector,
        WindowSpecification window)
        where TRight : class
        where TResult : class
    {
        if (rightStream == null) throw new ArgumentNullException(nameof(rightStream));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));
        if (window == null) throw new ArgumentNullException(nameof(window));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result stream
        string resultStreamName = $"{Name}_{((KsqlStream<TRight>)rightStream).Name}_join_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlStream<TRight>)rightStream).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            ((KsqlStream<TRight>)rightStream).Name,
            joinCondition,
            window.ToKsqlString());

        // Create a new stream for the join result
        // In a real implementation, this would execute the KSQL statement to create the join
        var resultStream = new KsqlJoinStream<T, TRight, TResult>(
            resultStreamName,
            _context,
            _schemaManager,
            this,
            (KsqlStream<TRight>)rightStream,
            joinOperation,
            resultSelector);

        return resultStream;
    }

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
    public IKsqlStream<TResult> Join<TRight, TKey, TResult>(
        IKsqlTable<TRight> table,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector)
        where TRight : class
        where TResult : class
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result stream
        string resultStreamName = $"{Name}_{((KsqlTable<TRight>)table).Name}_join_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TRight>)table).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            ((KsqlTable<TRight>)table).Name,
            joinCondition);

        // Create a new stream for the join result
        // In a real implementation, this would execute the KSQL statement to create the join
        var resultStream = new KsqlJoinStream<T, TRight, TResult>(
            resultStreamName,
            _context,
            _schemaManager,
            this,
            table,
            joinOperation,
            resultSelector);

        return resultStream;
    }

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
    public IKsqlStream<TResult> LeftJoin<TRight, TKey, TResult>(
        IKsqlTable<TRight> table,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector)
        where TRight : class
        where TResult : class
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result stream
        string resultStreamName = $"{Name}_{((KsqlTable<TRight>)table).Name}_leftjoin_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TRight>)table).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Left,
            Name,
            ((KsqlTable<TRight>)table).Name,
            joinCondition);

        // Create a new stream for the join result
        // In a real implementation, this would execute the KSQL statement to create the join
        var resultStream = new KsqlJoinStream<T, TRight, TResult>(
            resultStreamName,
            _context,
            _schemaManager,
            this,
            table,
            joinOperation,
            resultSelector);

        return resultStream;
    }

    private static string ExtractPropertyName<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertySelector)
    {
        if (propertySelector.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("The expression must be a property selector.", nameof(propertySelector));
    }

}

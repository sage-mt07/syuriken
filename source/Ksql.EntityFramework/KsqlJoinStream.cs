using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Ksql.EntityFramework;
using System.Xml.Linq;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Schema;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework;

/// <summary>
/// Represents the result of a join operation on KSQL streams.
/// </summary>
/// <typeparam name="TLeft">The type of entity in the left stream.</typeparam>
/// <typeparam name="TRight">The type of entity in the right stream or table.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
internal class KsqlJoinStream<TLeft, TRight, TResult> : IKsqlStream<TResult>
    where TLeft : class
    where TRight : class
    where TResult : class
{
    private readonly KsqlDbContext _context;
    private readonly SchemaManager _schemaManager;
    private readonly JoinOperation _joinOperation;
    private readonly Expression<Func<TLeft, TRight, TResult>> _resultSelector;
    private readonly object _leftSource; // IKsqlStream<TLeft> or KsqlStream<TLeft>
    private readonly object _rightSource; // Can be IKsqlStream<TRight>, IKsqlTable<TRight>, KsqlStream<TRight>, or KsqlTable<TRight>
    private ErrorAction _errorAction = ErrorAction.Stop;

    /// <summary>
    /// Gets the name of the result stream.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the provider.
    /// </summary>
    public Type ElementType => typeof(TResult);

    /// <summary>
    /// Gets the query expression.
    /// </summary>
    public Expression Expression => Expression.Constant(this);

    /// <summary>
    /// Gets the query provider.
    /// </summary>
    public IQueryProvider Provider => new KsqlQueryProvider();

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlJoinStream{TLeft, TRight, TResult}"/> class.
    /// </summary>
    /// <param name="name">The name of the result stream.</param>
    /// <param name="context">The database context.</param>
    /// <param name="schemaManager">The schema manager.</param>
    /// <param name="leftSource">The left stream.</param>
    /// <param name="rightSource">The right stream or table.</param>
    /// <param name="joinOperation">The join operation.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    public KsqlJoinStream(
        string name,
        KsqlDbContext context,
        SchemaManager schemaManager,
        object leftSource,
        object rightSource,
        JoinOperation joinOperation,
        Expression<Func<TLeft, TRight, TResult>> resultSelector)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        _leftSource = leftSource ?? throw new ArgumentNullException(nameof(leftSource));
        _rightSource = rightSource ?? throw new ArgumentNullException(nameof(rightSource));
        _joinOperation = joinOperation ?? throw new ArgumentNullException(nameof(joinOperation));
        _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));

        // In a real implementation, this would create the KSQL join stream
        CreateJoinStream();
    }

    private void CreateJoinStream()
    {
        // In a real implementation, this would execute a KSQL statement to create the join
        // For example:
        // CREATE STREAM result_stream AS
        // SELECT * FROM left_stream JOIN right_stream
        // ON left_stream.key = right_stream.key
        // WITHIN 1 HOURS;

        Console.WriteLine($"Creating join stream: {Name}");
        Console.WriteLine($"Join operation: {_joinOperation.ToKsqlString()}");
    }

    /// <summary>
    /// Produces a single entity to the stream with an auto-generated key.
    /// </summary>
    /// <param name="entity">The entity to produce.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating the offset of the produced record.</returns>
    public Task<long> ProduceAsync(TResult entity)
    {
        // This operation is not directly supported for join results
        // In a real implementation, you would need to produce to the source streams
        throw new NotSupportedException("Direct production to a join result stream is not supported.");
    }

    /// <summary>
    /// Produces a single entity to the stream with the specified key.
    /// </summary>
    /// <param name="key">The key for the record.</param>
    /// <param name="entity">The entity to produce.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating the offset of the produced record.</returns>
    public Task<long> ProduceAsync(string key, TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct production to a join result stream is not supported.");
    }

    /// <summary>
    /// Produces multiple entities to the stream in a batch.
    /// </summary>
    /// <param name="entities">The entities to produce.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task ProduceBatchAsync(IEnumerable<TResult> entities)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct production to a join result stream is not supported.");
    }

    /// <summary>
    /// Subscribes to the stream and receives entities as they are produced.
    /// </summary>
    /// <returns>An asynchronous enumerable of entities.</returns>
    public async IAsyncEnumerable<TResult> SubscribeAsync()
    {
        // In a real implementation, this would subscribe to the join result stream
        // For now, we return an empty enumerable
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// Configures a watermark for this stream based on the timestamp property.
    /// </summary>
    /// <param name="timestampSelector">A function to select the timestamp property.</param>
    /// <param name="maxOutOfOrderness">The maximum out-of-orderness to allow.</param>
    /// <returns>The stream with watermark configured.</returns>
    public IKsqlStream<TResult> WithWatermark<TTimestamp>(Expression<Func<TResult, TTimestamp>> timestampSelector, TimeSpan maxOutOfOrderness)
    {
        // In a real implementation, this would configure a watermark on the stream
        return this;
    }

    /// <summary>
    /// Configures error handling for this stream.
    /// </summary>
    /// <param name="errorAction">The action to take when an error occurs.</param>
    /// <returns>The stream with error handling configured.</returns>
    public IKsqlStream<TResult> OnError(ErrorAction errorAction)
    {
        _errorAction = errorAction;
        return this;
    }

    /// <summary>
    /// Creates a windowed stream using a tumbling window.
    /// </summary>
    /// <param name="window">The window specification.</param>
    /// <returns>A windowed stream.</returns>
    public IWindowedKsqlStream<TResult> Window(Windows.WindowSpecification window)
    {
        return new WindowedKsqlStream<TResult>(this, window);
    }

    /// <summary>
    /// Observes changes to the stream and receives change notifications.
    /// </summary>
    /// <returns>An asynchronous enumerable of change notifications.</returns>
    public async IAsyncEnumerable<ChangeNotification<TResult>> ObserveChangesAsync()
    {
        // In a real implementation, this would observe changes to the join result stream
        // For now, we return an empty enumerable
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// Adds a stream entity to be saved when SaveChanges is called.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    public void Add(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct addition to a join result stream is not supported.");
    }

    /// <summary>
    /// Gets an enumerator for the elements in the stream.
    /// </summary>
    /// <returns>An enumerator for the elements in the stream.</returns>
    public IEnumerator<TResult> GetEnumerator()
    {
        // This is a placeholder implementation for enumerating a stream
        // In a real implementation, this would execute a query against the stream
        return Enumerable.Empty<TResult>().GetEnumerator();
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
    /// Joins this stream with another stream within a specified window.
    /// </summary>
    /// <typeparam name="TJoinRight">The type of entity in the right stream.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TJoinResult">The type of the result.</typeparam>
    /// <param name="rightStream">The right stream to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this stream's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right stream's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <param name="window">The window specification for the join.</param>
    /// <returns>A stream containing the joined elements.</returns>
    public IKsqlStream<TJoinResult> Join<TJoinRight, TKey, TJoinResult>(
        IKsqlStream<TJoinRight> rightStream,
        Expression<Func<TResult, TKey>> leftKeySelector,
        Expression<Func<TJoinRight, TKey>> rightKeySelector,
        Expression<Func<TResult, TJoinRight, TJoinResult>> resultSelector,
        Windows.WindowSpecification window)
        where TJoinRight : class
        where TJoinResult : class
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
        string resultStreamName = $"{Name}_{((KsqlStream<TJoinRight>)rightStream).Name}_join_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlStream<TJoinRight>)rightStream).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            ((KsqlStream<TJoinRight>)rightStream).Name,
            joinCondition,
            window.ToKsqlString());

        // Create a new stream for the join result
        var resultStream = new KsqlJoinStream<TResult, TJoinRight, TJoinResult>(
            resultStreamName,
            _context,
            _schemaManager,
            this,
            (KsqlStream<TJoinRight>)rightStream,
            joinOperation,
            resultSelector);

        return resultStream;
    }

    /// <summary>
    /// Joins this stream with a table.
    /// </summary>
    /// <typeparam name="TJoinRight">The type of entity in the table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TJoinResult">The type of the result.</typeparam>
    /// <param name="table">The table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this stream's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A stream containing the joined elements.</returns>
    public IKsqlStream<TJoinResult> Join<TJoinRight, TKey, TJoinResult>(
        IKsqlTable<TJoinRight> table,
        Expression<Func<TResult, TKey>> leftKeySelector,
        Expression<Func<TJoinRight, TKey>> rightKeySelector,
        Expression<Func<TResult, TJoinRight, TJoinResult>> resultSelector)
        where TJoinRight : class
        where TJoinResult : class
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result stream
        string resultStreamName = $"{Name}_{((KsqlTable<TJoinRight>)table).Name}_join_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TJoinRight>)table).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            ((KsqlTable<TJoinRight>)table).Name,
            joinCondition);

        // Create a new stream for the join result
        var resultStream = new KsqlJoinStream<TResult, TJoinRight, TJoinResult>(
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
    /// <typeparam name="TJoinRight">The type of entity in the table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TJoinResult">The type of the result.</typeparam>
    /// <param name="table">The table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this stream's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A stream containing the joined elements.</returns>
    public IKsqlStream<TJoinResult> LeftJoin<TJoinRight, TKey, TJoinResult>(
        IKsqlTable<TJoinRight> table,
        Expression<Func<TResult, TKey>> leftKeySelector,
        Expression<Func<TJoinRight, TKey>> rightKeySelector,
        Expression<Func<TResult, TJoinRight, TJoinResult>> resultSelector)
        where TJoinRight : class
        where TJoinResult : class
    {
        if (table == null) throw new ArgumentNullException(nameof(table));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result stream
        string resultStreamName = $"{Name}_{((KsqlTable<TJoinRight>)table).Name}_leftjoin_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TJoinRight>)table).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Left,
            Name,
            ((KsqlTable<TJoinRight>)table).Name,
            joinCondition);

        // Create a new stream for the join result
        var resultStream = new KsqlJoinStream<TResult, TJoinRight, TJoinResult>(
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
  
    /// <summary>
    /// Executes a projection operation on this stream.
    /// </summary>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    /// <param name="selector">A function to select the output from this stream's elements.</param>
    /// <returns>A stream with the projected elements.</returns>
    public IKsqlStream<TOutput> Select<TOutput>(Expression<Func<TResult, TOutput>> selector)
        where TOutput : class
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        // In a real implementation, this would create a new stream with the projection
        // For now, we'll simulate it with another join stream
        string resultStreamName = $"{Name}_select_{Guid.NewGuid():N}";

        // Create a special projection join operation
        var projectionOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            "PROJECTION",
            "true");

        // Create a result selector that applies the projection
        Expression<Func<TResult, object, TOutput>> resultSelector =
            (source, _) => selector.Compile()(source);

        // Create a new stream for the projection result
        var resultStream = new KsqlJoinStream<TResult, object, TOutput>(
            resultStreamName,
            _context,
            _schemaManager,
            this,
            new object(), // Dummy right source
            projectionOperation,
            resultSelector);

        return resultStream;
    }

    /// <summary>
    /// Filters the elements of this stream based on a predicate.
    /// </summary>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>A stream that contains elements from the input stream that satisfy the condition.</returns>
    public IKsqlStream<TResult> Where(Expression<Func<TResult, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        // In a real implementation, this would create a new stream with the filter
        // For now, we'll simulate it with another join stream
        string resultStreamName = $"{Name}_where_{Guid.NewGuid():N}";

        // Create a special filter join operation
        var filterOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            "FILTER",
            $"WHERE {ConvertPredicateToKSQL(predicate)}");

        // Create a result selector that just returns the source
        Expression<Func<TResult, object, TResult>> resultSelector =
            (source, _) => source;

        // Create a new stream for the filter result
        var resultStream = new KsqlJoinStream<TResult, object, TResult>(
            resultStreamName,
            _context,
            _schemaManager,
            this,
            new object(), // Dummy right source
            filterOperation,
            resultSelector);

        return resultStream;
    }

    /// <summary>
    /// Groups the elements of this stream by a key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <param name="keySelector">A function to extract the key from each element.</param>
    /// <returns>A stream with the grouped elements.</returns>
    public IKsqlGroupedStream<TKey, TResult> GroupBy<TKey>(Expression<Func<TResult, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        // Extract property name from key selector
        string keyProperty = ExtractPropertyName(keySelector);

        // In a real implementation, this would create a grouped stream
        // For now, we'll just return a dummy implementation
        return new KsqlGroupedStream<TKey, TResult>(
            $"{Name}_groupby_{keyProperty}",
            _context,
            _schemaManager,
            this,
            keySelector);
    }

    /// <summary>
    /// Creates a new join result type for aggregation operations.
    /// </summary>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    /// <param name="aggregateExpression">The aggregation expression in KSQL.</param>
    /// <param name="resultCreator">A function to create the result from the aggregated value.</param>
    /// <returns>A stream with the aggregated results.</returns>
    public IKsqlStream<TOutput> Aggregate<TOutput>(
        string aggregateExpression,
        Func<object, TOutput> resultCreator)
        where TOutput : class
    {
        if (string.IsNullOrEmpty(aggregateExpression)) throw new ArgumentNullException(nameof(aggregateExpression));
        if (resultCreator == null) throw new ArgumentNullException(nameof(resultCreator));

        // In a real implementation, this would create a new stream with the aggregation
        // For now, we'll simulate it with another join stream
        string resultStreamName = $"{Name}_aggregate_{Guid.NewGuid():N}";

        // Create a special aggregation join operation
        var aggregateOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            "AGGREGATE",
            aggregateExpression);

        // Create a result selector that applies the aggregation
        Expression<Func<TResult, object, TOutput>> resultSelector =
            (source, aggregatedValue) => resultCreator(aggregatedValue);

        // Create a new stream for the aggregation result
        var resultStream = new KsqlJoinStream<TResult, object, TOutput>(
            resultStreamName,
            _context,
            _schemaManager,
            this,
            new object(), // Dummy right source
            aggregateOperation,
            resultSelector);

        return resultStream;
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
        Action<TResult> onNext,
        Action<Exception>? onError = null,
        Action? onCompleted = null,
        CancellationToken cancellationToken = default)
    {
        if (onNext == null) throw new ArgumentNullException(nameof(onNext));

        // Create a subscription that processes entities as they are produced
        var subscription = new KsqlSubscription<TResult>(
            async () =>
            {
                try
                {
                    await foreach (var entity in SubscribeAsync().WithCancellation(cancellationToken))
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
    /// Converts a predicate expression to a KSQL WHERE clause.
    /// </summary>
    /// <param name="predicate">The predicate expression to convert.</param>
    /// <returns>A KSQL WHERE clause.</returns>
    private string ConvertPredicateToKSQL<T>(Expression<Func<T, bool>> predicate)
    {
        // This is a simplified implementation that would need to be expanded
        // to handle all possible predicate expressions
        if (predicate.Body is BinaryExpression binaryExpression)
        {
            string left = ConvertExpressionToKSQL(binaryExpression.Left);
            string right = ConvertExpressionToKSQL(binaryExpression.Right);
            string op = GetKsqlOperator(binaryExpression.NodeType);

            return $"{left} {op} {right}";
        }

        // Default case, just return a placeholder
        return "1=1";
    }

    /// <summary>
    /// Converts an expression to a KSQL expression.
    /// </summary>
    /// <param name="expression">The expression to convert.</param>
    /// <returns>A KSQL expression.</returns>
    private string ConvertExpressionToKSQL(Expression expression)
    {
        if (expression is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }
        else if (expression is ConstantExpression constantExpression)
        {
            var value = constantExpression.Value;
            if (value is string)
            {
                return $"'{value}'";
            }
            else
            {
                return value?.ToString() ?? "NULL";
            }
        }

        // Default case
        return expression.ToString();
    }

    /// <summary>
    /// Gets the KSQL operator for an expression node type.
    /// </summary>
    /// <param name="nodeType">The expression node type.</param>
    /// <returns>The KSQL operator.</returns>
    private string GetKsqlOperator(ExpressionType nodeType)
    {
        return nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            _ => throw new NotSupportedException($"Operator {nodeType} is not supported in KSQL.")
        };
    }
}

/// <summary>
/// Represents a grouped stream in KSQL.
/// </summary>
/// <typeparam name="TKey">The type of the grouping key.</typeparam>
/// <typeparam name="TElement">The type of the elements in the stream.</typeparam>
internal class KsqlGroupedStream<TKey, TElement> : IKsqlGroupedStream<TKey, TElement>
    where TElement : class
{
    private readonly KsqlDbContext _context;
    private readonly SchemaManager _schemaManager;
    private readonly IKsqlStream<TElement> _source;
    private readonly Expression<Func<TElement, TKey>> _keySelector;

    /// <summary>
    /// Gets the name of the grouped stream.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the type of the provider.
    /// </summary>
    public Type ElementType => typeof(TElement);

    /// <summary>
    /// Gets the query expression.
    /// </summary>
    public Expression Expression => Expression.Constant(this);

    /// <summary>
    /// Gets the query provider.
    /// </summary>
    public IQueryProvider Provider => new KsqlQueryProvider();

    /// <summary>
    /// Gets the key selector for the grouped stream.
    /// </summary>
    public Expression<Func<TElement, TKey>> KeySelector => _keySelector;

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlGroupedStream{TKey, TElement}"/> class.
    /// </summary>
    /// <param name="name">The name of the grouped stream.</param>
    /// <param name="context">The database context.</param>
    /// <param name="schemaManager">The schema manager.</param>
    /// <param name="source">The source stream.</param>
    /// <param name="keySelector">The key selector.</param>
    public KsqlGroupedStream(
        string name,
        KsqlDbContext context,
        SchemaManager schemaManager,
        IKsqlStream<TElement> source,
        Expression<Func<TElement, TKey>> keySelector)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    /// <summary>
    /// Aggregates the elements of the stream.
    /// </summary>
    /// <typeparam name="TAccumulate">The type of the accumulator.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="accumulator">A function to update the accumulator based on an element.</param>
    /// <param name="resultSelector">A function to transform the accumulator into the result type.</param>
    /// <returns>A table with the aggregated results.</returns>
    public IKsqlTable<TResult> Aggregate<TAccumulate, TResult>(
        TAccumulate seed,
        Expression<Func<TAccumulate, TElement, TAccumulate>> accumulator,
        Expression<Func<TKey, TAccumulate, TResult>> resultSelector)
        where TResult : class
    {
        // In a real implementation, this would create a new table with the aggregation
        // For now, we'll just return a null
        throw new NotImplementedException("Aggregation is not implemented yet.");
    }

    /// <summary>
    /// Gets an enumerator for the elements in the grouped stream.
    /// </summary>
    /// <returns>An enumerator for the elements in the grouped stream.</returns>
    public IEnumerator<TElement> GetEnumerator()
    {
        return _source.GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator for the elements in the grouped stream.
    /// </summary>
    /// <returns>An enumerator for the elements in the grouped stream.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

/// <summary>
/// Represents a grouped stream in KSQL.
/// </summary>
/// <typeparam name="TKey">The type of the grouping key.</typeparam>
/// <typeparam name="TElement">The type of the elements in the stream.</typeparam>
public interface IKsqlGroupedStream<TKey, TElement> : IQueryable<TElement>
    where TElement : class
{
    /// <summary>
    /// Gets the key selector for the grouped stream.
    /// </summary>
    Expression<Func<TElement, TKey>> KeySelector { get; }

    /// <summary>
    /// Aggregates the elements of the stream.
    /// </summary>
    /// <typeparam name="TAccumulate">The type of the accumulator.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="seed">The initial accumulator value.</param>
    /// <param name="accumulator">A function to update the accumulator based on an element.</param>
    /// <param name="resultSelector">A function to transform the accumulator into the result type.</param>
    /// <returns>A table with the aggregated results.</returns>
    IKsqlTable<TResult> Aggregate<TAccumulate, TResult>(
        TAccumulate seed,
        Expression<Func<TAccumulate, TElement, TAccumulate>> accumulator,
        Expression<Func<TKey, TAccumulate, TResult>> resultSelector)
        where TResult : class;
}

/// <summary>
/// Represents a subscription to a KSQL stream.
/// </summary>
/// <typeparam name="T">The type of entity in the stream.</typeparam>
internal class KsqlSubscription<T> : IDisposable
    where T : class
{
    private readonly Func<Task> _subscriptionTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _runningTask;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlSubscription{T}"/> class.
    /// </summary>
    /// <param name="subscriptionTask">The task to run for the subscription.</param>
    public KsqlSubscription(Func<Task> subscriptionTask)
    {
        _subscriptionTask = subscriptionTask ?? throw new ArgumentNullException(nameof(subscriptionTask));
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Starts the subscription.
    /// </summary>
    public void Start()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KsqlSubscription<T>));
        if (_runningTask != null) throw new InvalidOperationException("Subscription is already running.");

        _runningTask = Task.Run(_subscriptionTask);
    }

    /// <summary>
    /// Disposes the subscription, canceling any ongoing operations.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the subscription.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cancel the ongoing task
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }

            _disposed = true;
        }
    }
}

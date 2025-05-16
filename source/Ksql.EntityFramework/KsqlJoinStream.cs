using System.Collections;
using System.Linq.Expressions;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Schema;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework;

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

    public string Name { get; }

    public Type ElementType => typeof(TResult);

    public Expression Expression => Expression.Constant(this);

    public IQueryProvider Provider => new KsqlQueryProvider();

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

    public Task<long> ProduceAsync(TResult entity)
    {
        // This operation is not directly supported for join results
        // In a real implementation, you would need to produce to the source streams
        throw new NotSupportedException("Direct production to a join result stream is not supported.");
    }

    public Task<long> ProduceAsync(string key, TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct production to a join result stream is not supported.");
    }

    public Task ProduceBatchAsync(IEnumerable<TResult> entities)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct production to a join result stream is not supported.");
    }

    public async IAsyncEnumerable<TResult> SubscribeAsync()
    {
        // In a real implementation, this would subscribe to the join result stream
        // For now, we return an empty enumerable
        await Task.CompletedTask;
        yield break;
    }

    public IKsqlStream<TResult> WithWatermark<TTimestamp>(Expression<Func<TResult, TTimestamp>> timestampSelector, TimeSpan maxOutOfOrderness)
    {
        // In a real implementation, this would configure a watermark on the stream
        return this;
    }

    public IKsqlStream<TResult> OnError(ErrorAction errorAction)
    {
        _errorAction = errorAction;
        return this;
    }

    public IWindowedKsqlStream<TResult> Window(Windows.WindowSpecification window)
    {
        return new WindowedKsqlStream<TResult>(this, window);
    }
    public async IAsyncEnumerable<ChangeNotification<TResult>> ObserveChangesAsync()
    {
        // In a real implementation, this would observe changes to the join result stream
        // For now, we return an empty enumerable
        await Task.CompletedTask;
        yield break;
    }

    public void Add(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct addition to a join result stream is not supported.");
    }

    public IEnumerator<TResult> GetEnumerator()
    {
        // This is a placeholder implementation for enumerating a stream
        // In a real implementation, this would execute a query against the stream
        return Enumerable.Empty<TResult>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

   
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


    public IEnumerator<TElement> GetEnumerator()
    {
        return _source.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public interface IKsqlGroupedStream<TKey, TElement> : IQueryable<TElement>
    where TElement : class
{

    Expression<Func<TElement, TKey>> KeySelector { get; }

    IKsqlTable<TResult> Aggregate<TAccumulate, TResult>(
        TAccumulate seed,
        Expression<Func<TAccumulate, TElement, TAccumulate>> accumulator,
        Expression<Func<TKey, TAccumulate, TResult>> resultSelector)
        where TResult : class;
}


internal class KsqlSubscription<T> : IDisposable
    where T : class
{
    private readonly Func<Task> _subscriptionTask;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private Task? _runningTask;
    private bool _disposed;


    public KsqlSubscription(Func<Task> subscriptionTask)
    {
        _subscriptionTask = subscriptionTask ?? throw new ArgumentNullException(nameof(subscriptionTask));
        _cancellationTokenSource = new CancellationTokenSource();
    }


    public void Start()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(KsqlSubscription<T>));
        if (_runningTask != null) throw new InvalidOperationException("Subscription is already running.");

        _runningTask = Task.Run(_subscriptionTask);
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

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

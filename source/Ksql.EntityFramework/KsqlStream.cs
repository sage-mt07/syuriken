using System.Collections;
using System.Linq.Expressions;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Schema;
using Ksql.EntityFramework.Windows;

namespace Ksql.EntityFramework;

public class KsqlStream<T> : IKsqlStream<T> where T : class
{
    private readonly KsqlDbContext _context;
    private readonly SchemaManager _schemaManager;
    private readonly List<T> _pendingAdds = new List<T>();
    private ErrorAction _errorAction = ErrorAction.Stop;

    public string Name { get; }

    public Type ElementType => typeof(T);

    public Expression Expression => Expression.Constant(this);

    public IQueryProvider Provider => new KsqlQueryProvider();

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

  
    public async Task<long> ProduceAsync(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // 複合キーを含む可能性のあるキーを取得
        string key = GetEntityKey(entity) ?? Guid.NewGuid().ToString();

        using var producer = GetProducer();
        var result = await producer.ProduceAsync(key, entity);
        return result.Offset.Value;
    }


    public async Task<long> ProduceAsync(string key, T entity)
    {
        using var producer = GetProducer();
        var result = await producer.ProduceAsync(key, entity);
        return result.Offset.Value;
    }

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


    public void Add(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _pendingAdds.Add(entity);
        _context.AddToPendingChanges(entity);
    }

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

    public IKsqlStream<T> WithWatermark<TTimestamp>(Expression<Func<T, TTimestamp>> timestampSelector, TimeSpan maxOutOfOrderness)
    {
        // This is a placeholder implementation for configuring a watermark
        // In a real implementation, this would configure a watermark on the stream
        return this;
    }


    public IKsqlStream<T> OnError(ErrorAction errorAction)
    {
        _errorAction = errorAction;
        return this;
    }

    public IWindowedKsqlStream<T> Window(WindowSpecification window)
    {
        return new WindowedKsqlStream<T>(this, window);
    }



    public IEnumerator<T> GetEnumerator()
    {
        // This is a placeholder implementation for enumerating a stream
        // In a real implementation, this would execute a query against the stream
        return Enumerable.Empty<T>().GetEnumerator();
    }


    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

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

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
internal class KsqlStream<T> : IKsqlStream<T> where T : class
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
}

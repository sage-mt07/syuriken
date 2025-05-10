using System.Collections;
using System.Linq.Expressions;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Schema;

namespace Ksql.EntityFramework;

/// <summary>
/// Implementation of a KSQL table.
/// </summary>
/// <typeparam name="T">The type of entity in the table.</typeparam>
internal class KsqlTable<T> : IKsqlTable<T> where T : class
{
    private readonly KsqlDbContext _context;
    private readonly SchemaManager _schemaManager;
    private readonly TableOptions _options;

    /// <summary>
    /// Gets the name of the table.
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
    /// Initializes a new instance of the <see cref="KsqlTable{T}"/> class.
    /// </summary>
    /// <param name="name">The name of the table.</param>
    /// <param name="context">The database context.</param>
    /// <param name="schemaManager">The schema manager.</param>
    public KsqlTable(string name, KsqlDbContext context, SchemaManager schemaManager)
        : this(name, context, schemaManager, new TableOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="KsqlTable{T}"/> class with the specified options.
    /// </summary>
    /// <param name="name">The name of the table.</param>
    /// <param name="context">The database context.</param>
    /// <param name="schemaManager">The schema manager.</param>
    /// <param name="options">The options for the table.</param>
    public KsqlTable(string name, KsqlDbContext context, SchemaManager schemaManager, TableOptions options)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
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
    /// Gets an entity from the table by its key.
    /// </summary>
    /// <param name="key">The primary key of the entity.</param>
    /// <returns>A task representing the asynchronous operation, with the result containing the entity or null if not found.</returns>
    public async Task<T?> GetAsync(object key)
    {
        // For a real implementation, we would use the KSQL pull query API
        // For now, we use a simplistic approach that returns the first entity with the matching key from the topic
        Console.WriteLine($"Getting entity from table '{Name}' with key '{key}'");

        var consumer = GetConsumer();
        try
        {
            // Set a reasonable timeout for finding the entity
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            foreach (var entity in consumer.Consume(cancellationTokenSource.Token))
            {
                // Check if the entity has the matching key
                var entityKey = GetEntityKey(entity);
                if (entityKey != null && entityKey.ToString() == key.ToString())
                {
                    return entity;
                }
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            // Timeout expired
            return null;
        }
        finally
        {
            consumer.Dispose();
        }
    }

    /// <summary>
    /// Finds an entity from the table by its key.
    /// </summary>
    /// <param name="key">The primary key of the entity.</param>
    /// <returns>A task representing the asynchronous operation, with the result containing the entity or null if not found.</returns>
    public Task<T?> FindAsync(object key)
    {
        return GetAsync(key);
    }

    /// <summary>
    /// Inserts an entity into the table.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating whether the insert was successful.</returns>
    public async Task<bool> InsertAsync(T entity)
    {
        try
        {
            var key = GetEntityKey(entity) ?? Guid.NewGuid().ToString();
            using var producer = GetProducer();
            await producer.ProduceAsync(key.ToString(), entity);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inserting entity into table '{Name}': {ex.Message}");
            return false;
        }
    }

    private object GetEntityKey(T entity)
    {
        // Find properties with [Key] attribute and get their values
        var keyProperties = typeof(T).GetProperties()
            .Where(p => p.GetCustomAttributes(typeof(Attributes.KeyAttribute), true).Any());

        foreach (var prop in keyProperties)
        {
            var value = prop.GetValue(entity);
            if (value != null)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds a table entity to be saved when SaveChanges is called.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    public void Add(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        _context.AddToPendingChanges(entity);
    }

    /// <summary>
    /// Removes a table entity to be deleted when SaveChanges is called.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        // Mark the entity for deletion in the context
        // This is a placeholder implementation - in a real implementation, this would mark the entity for deletion
    }

    /// <summary>
    /// Retrieves all entities from the table as a list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, with the result containing the list of entities.</returns>
    public async Task<List<T>> ToListAsync()
    {
        var result = new List<T>();
        var processedKeys = new HashSet<string>();

        var consumer = GetConsumer();
        try
        {
            // Set a reasonable timeout for consuming all entities
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            foreach (var entity in consumer.Consume(cancellationTokenSource.Token))
            {
                var key = GetEntityKey(entity)?.ToString();
                if (key != null && !processedKeys.Contains(key))
                {
                    processedKeys.Add(key);
                    result.Add(entity);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout expired - return what we've collected so far
        }
        finally
        {
            consumer.Dispose();
        }

        return result;
    }



    /// <summary>
    /// Gets a descriptor for this table.
    /// </summary>
    /// <returns>A table descriptor.</returns>
    internal TableDescriptor GetTableDescriptor()
    {
        var topicDescriptor = _schemaManager.GetTopicDescriptor<T>();

        return new TableDescriptor
        {
            Name = Name,
            TopicDescriptor = topicDescriptor,
            Options = _options
        };
    }

    /// <summary>
    /// Gets an enumerator for the elements in the table.
    /// </summary>
    /// <returns>An enumerator for the elements in the table.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        // This is a placeholder implementation for enumerating a table
        // In a real implementation, this would execute a query against the table
        return Enumerable.Empty<T>().GetEnumerator();
    }

    /// <summary>
    /// Gets an enumerator for the elements in the table.
    /// </summary>
    /// <returns>An enumerator for the elements in the table.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

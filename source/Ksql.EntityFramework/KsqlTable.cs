using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Schema;

namespace Ksql.EntityFramework;

/// <summary>
/// Implementation of a KSQL table.
/// </summary>
/// <typeparam name="T">The type of entity in the table.</typeparam>
public class KsqlTable<T> : IKsqlTable<T> where T : class
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

    private string GetEntityKey(T entity)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        return _schemaManager.CreateKeyString(entity);
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
    /// <summary>
    /// Joins this table with another table.
    /// </summary>
    /// <typeparam name="TRight">The type of entity in the right table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="rightTable">The right table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this table's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A table containing the joined elements.</returns>
    public IKsqlTable<TResult> Join<TRight, TKey, TResult>(
        IKsqlTable<TRight> rightTable,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector)
        where TRight : class
        where TResult : class
    {
        if (rightTable == null) throw new ArgumentNullException(nameof(rightTable));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result table
        string resultTableName = $"{Name}_{((KsqlTable<TRight>)rightTable).Name}_join_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TRight>)rightTable).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            ((KsqlTable<TRight>)rightTable).Name,
            joinCondition);

        // Create a new table for the join result
        // In a real implementation, this would execute the KSQL statement to create the join
        var resultTable = new KsqlJoinTable<T, TRight, TResult>(
            resultTableName,
            _context,
            _schemaManager,
            this,
            (KsqlTable<TRight>)rightTable,
            joinOperation,
            resultSelector);

        return resultTable;
    }

    /// <summary>
    /// Left joins this table with another table.
    /// </summary>
    /// <typeparam name="TRight">The type of entity in the right table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="rightTable">The right table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this table's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A table containing the joined elements.</returns>
    public IKsqlTable<TResult> LeftJoin<TRight, TKey, TResult>(
        IKsqlTable<TRight> rightTable,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector)
        where TRight : class
        where TResult : class
    {
        if (rightTable == null) throw new ArgumentNullException(nameof(rightTable));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result table
        string resultTableName = $"{Name}_{((KsqlTable<TRight>)rightTable).Name}_leftjoin_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TRight>)rightTable).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Left,
            Name,
            ((KsqlTable<TRight>)rightTable).Name,
            joinCondition);

        // Create a new table for the join result
        var resultTable = new KsqlJoinTable<T, TRight, TResult>(
            resultTableName,
            _context,
            _schemaManager,
            this,
            (KsqlTable<TRight>)rightTable,
            joinOperation,
            resultSelector);

        return resultTable;
    }

    /// <summary>
    /// Full outer joins this table with another table.
    /// </summary>
    /// <typeparam name="TRight">The type of entity in the right table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="rightTable">The right table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this table's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A table containing the joined elements.</returns>
    public IKsqlTable<TResult> FullOuterJoin<TRight, TKey, TResult>(
        IKsqlTable<TRight> rightTable,
        Expression<Func<T, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector,
        Expression<Func<T, TRight, TResult>> resultSelector)
        where TRight : class
        where TResult : class
    {
        if (rightTable == null) throw new ArgumentNullException(nameof(rightTable));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result table
        string resultTableName = $"{Name}_{((KsqlTable<TRight>)rightTable).Name}_fullouterjoin_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TRight>)rightTable).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.FullOuter,
            Name,
            ((KsqlTable<TRight>)rightTable).Name,
            joinCondition);

        // Create a new table for the join result
        var resultTable = new KsqlJoinTable<T, TRight, TResult>(
            resultTableName,
            _context,
            _schemaManager,
            this,
            (KsqlTable<TRight>)rightTable,
            joinOperation,
            resultSelector);

        return resultTable;
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
// JOIN拡張ヘルパー
public static class JoinExtensions
{
    /// <summary>
    /// Creates a join condition for properties with matching names in both tables.
    /// </summary>
    public static string CreateJoinCondition(string leftTableName, string rightTableName, IEnumerable<string> keyProperties)
    {
        if (keyProperties == null) throw new ArgumentNullException(nameof(keyProperties));

        var conditions = keyProperties.Select(prop =>
            $"{leftTableName}.{prop} = {rightTableName}.{prop}");

        return string.Join(" AND ", conditions);
    }

    /// <summary>
    /// Creates a join condition from key selectors.
    /// </summary>
    public static string CreateJoinCondition<TLeft, TRight, TKey>(
        string leftTableName,
        string rightTableName,
        Expression<Func<TLeft, TKey>> leftKeySelector,
        Expression<Func<TRight, TKey>> rightKeySelector)
    {
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));

        // アノニマス型を使った複合キーの場合
        if (typeof(TKey).IsAnonymousType())
        {
            var leftProperties = ExtractAnonymousProperties(leftKeySelector);
            var rightProperties = ExtractAnonymousProperties(rightKeySelector);

            if (leftProperties.Count != rightProperties.Count)
                throw new InvalidOperationException("Key property count mismatch for join operation.");

            var conditions = new List<string>();
            for (int i = 0; i < leftProperties.Count; i++)
            {
                conditions.Add($"{leftTableName}.{leftProperties[i]} = {rightTableName}.{rightProperties[i]}");
            }

            return string.Join(" AND ", conditions);
        }
        else
        {
            // 単一キーの場合
            string leftKeyProperty = ExtractPropertyName(leftKeySelector);
            string rightKeyProperty = ExtractPropertyName(rightKeySelector);
            return $"{leftTableName}.{leftKeyProperty} = {rightTableName}.{rightKeyProperty}";
        }
    }

    // アノニマス型のプロパティ名を抽出するヘルパーメソッド
    private static List<string> ExtractAnonymousProperties<T, TKey>(Expression<Func<T, TKey>> keySelector)
    {
        if (keySelector.Body is NewExpression newExpression)
        {
            var properties = new List<string>();

            for (int i = 0; i < newExpression.Arguments.Count; i++)
            {
                if (newExpression.Arguments[i] is MemberExpression memberExpression)
                {
                    properties.Add(memberExpression.Member.Name);
                }
                else
                {
                    throw new ArgumentException($"Unsupported expression in composite key: {newExpression.Arguments[i]}");
                }
            }

            return properties;
        }

        throw new ArgumentException("Expected an anonymous type creation expression", nameof(keySelector));
    }

    // プロパティ名を抽出するヘルパーメソッド
    private static string ExtractPropertyName<T, TProperty>(Expression<Func<T, TProperty>> propertySelector)
    {
        if (propertySelector.Body is MemberExpression memberExpression)
        {
            return memberExpression.Member.Name;
        }

        throw new ArgumentException("The expression must be a property selector.", nameof(propertySelector));
    }

    // アノニマス型かどうかを判定するヘルパーメソッド
    private static bool IsAnonymousType(this Type type)
    {
        return Attribute.IsDefined(type, typeof(CompilerGeneratedAttribute), false)
            && type.IsGenericType
            && type.Name.Contains("AnonymousType")
            && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
            && type.Attributes.HasFlag(TypeAttributes.NotPublic);
    }
}
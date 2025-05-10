using System.Collections;
using System.Linq.Expressions;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Schema;

namespace Ksql.EntityFramework;

/// <summary>
/// Represents the result of a join operation on KSQL tables.
/// </summary>
/// <typeparam name="TLeft">The type of entity in the left table.</typeparam>
/// <typeparam name="TRight">The type of entity in the right table.</typeparam>
/// <typeparam name="TResult">The type of the result.</typeparam>
internal class KsqlJoinTable<TLeft, TRight, TResult> : IKsqlTable<TResult>
    where TLeft : class
    where TRight : class
    where TResult : class
{
    private readonly KsqlDbContext _context;
    private readonly SchemaManager _schemaManager;
    private readonly JoinOperation _joinOperation;
    private readonly Expression<Func<TLeft, TRight, TResult>> _resultSelector;
    private readonly IKsqlTable<TLeft> _leftTable;
    private readonly IKsqlTable<TRight> _rightTable;

    /// <summary>
    /// Gets the name of the result table.
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
    /// Initializes a new instance of the <see cref="KsqlJoinTable{TLeft, TRight, TResult}"/> class.
    /// </summary>
    /// <param name="name">The name of the result table.</param>
    /// <param name="context">The database context.</param>
    /// <param name="schemaManager">The schema manager.</param>
    /// <param name="leftTable">The left table.</param>
    /// <param name="rightTable">The right table.</param>
    /// <param name="joinOperation">The join operation.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    public KsqlJoinTable(
        string name,
        KsqlDbContext context,
        SchemaManager schemaManager,
        IKsqlTable<TLeft> leftTable,
        IKsqlTable<TRight> rightTable,
        JoinOperation joinOperation,
        Expression<Func<TLeft, TRight, TResult>> resultSelector)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        _leftTable = leftTable ?? throw new ArgumentNullException(nameof(leftTable));
        _rightTable = rightTable ?? throw new ArgumentNullException(nameof(rightTable));
        _joinOperation = joinOperation ?? throw new ArgumentNullException(nameof(joinOperation));
        _resultSelector = resultSelector ?? throw new ArgumentNullException(nameof(resultSelector));

        // In a real implementation, this would create the KSQL join table
        CreateJoinTable();
    }

    private void CreateJoinTable()
    {
        // In a real implementation, this would execute a KSQL statement to create the join
        // For example:
        // CREATE TABLE result_table AS
        // SELECT * FROM left_table JOIN right_table
        // ON left_table.key = right_table.key;

        Console.WriteLine($"Creating join table: {Name}");
        Console.WriteLine($"Join operation: {_joinOperation.ToKsqlString()}");
    }

    /// <summary>
    /// Gets an entity from the table by its key.
    /// </summary>
    /// <param name="key">The primary key of the entity.</param>
    /// <returns>A task representing the asynchronous operation, with the result containing the entity or null if not found.</returns>
    public async Task<TResult?> GetAsync(object key)
    {
        // In a real implementation, this would execute a KSQL query to get the entity
        // For now, we return null
        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Finds an entity from the table by its key.
    /// </summary>
    /// <param name="key">The primary key of the entity.</param>
    /// <returns>A task representing the asynchronous operation, with the result containing the entity or null if not found.</returns>
    public Task<TResult?> FindAsync(object key)
    {
        return GetAsync(key);
    }

    /// <summary>
    /// Inserts an entity into the table.
    /// </summary>
    /// <param name="entity">The entity to insert.</param>
    /// <returns>A task representing the asynchronous operation, with the result indicating whether the insert was successful.</returns>
    public Task<bool> InsertAsync(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct insertion to a join result table is not supported.");
    }

    /// <summary>
    /// Retrieves all entities from the table as a list.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, with the result containing the list of entities.</returns>
    public async Task<List<TResult>> ToListAsync()
    {
        // In a real implementation, this would execute a KSQL query to get all entities
        // For now, we return an empty list
        await Task.CompletedTask;
        return new List<TResult>();
    }

    /// <summary>
    /// Observes changes to the table and receives change notifications.
    /// </summary>
    /// <returns>An asynchronous enumerable of change notifications.</returns>
    public async IAsyncEnumerable<ChangeNotification<TResult>> ObserveChangesAsync()
    {
        // In a real implementation, this would observe changes to the join result table
        // For now, we return an empty enumerable
        await Task.CompletedTask;
        yield break;
    }

    /// <summary>
    /// Adds a table entity to be saved when SaveChanges is called.
    /// </summary>
    /// <param name="entity">The entity to add.</param>
    public void Add(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct addition to a join result table is not supported.");
    }

    /// <summary>
    /// Removes a table entity to be deleted when SaveChanges is called.
    /// </summary>
    /// <param name="entity">The entity to remove.</param>
    public void Remove(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct removal from a join result table is not supported.");
    }

    /// <summary>
    /// Gets an enumerator for the elements in the table.
    /// </summary>
    /// <returns>An enumerator for the elements in the table.</returns>
    public IEnumerator<TResult> GetEnumerator()
    {
        // This is a placeholder implementation for enumerating a table
        // In a real implementation, this would execute a query against the table
        return Enumerable.Empty<TResult>().GetEnumerator();
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
    /// <typeparam name="TJoinRight">The type of entity in the right table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TJoinResult">The type of the result.</typeparam>
    /// <param name="rightTable">The right table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this table's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A table containing the joined elements.</returns>
    public IKsqlTable<TJoinResult> Join<TJoinRight, TKey, TJoinResult>(
        IKsqlTable<TJoinRight> rightTable,
        Expression<Func<TResult, TKey>> leftKeySelector,
        Expression<Func<TJoinRight, TKey>> rightKeySelector,
        Expression<Func<TResult, TJoinRight, TJoinResult>> resultSelector)
        where TJoinRight : class
        where TJoinResult : class
    {
        if (rightTable == null) throw new ArgumentNullException(nameof(rightTable));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result table
        string resultTableName = $"{Name}_{((KsqlTable<TJoinRight>)rightTable).Name}_join_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TJoinRight>)rightTable).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Inner,
            Name,
            ((KsqlTable<TJoinRight>)rightTable).Name,
            joinCondition);

        // Create a new table for the join result
        var result = new KsqlJoinTable<TResult, TJoinRight, TJoinResult>(
            resultTableName,
            _context,
            _schemaManager,
            this,
            (KsqlTable<TJoinRight>)rightTable,
            joinOperation,
            resultSelector);

        return result;
    }

    /// <summary>
    /// Left joins this table with another table.
    /// </summary>
    /// <typeparam name="TJoinRight">The type of entity in the right table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TJoinResult">The type of the result.</typeparam>
    /// <param name="rightTable">The right table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this table's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A table containing the joined elements.</returns>
    public IKsqlTable<TJoinResult> LeftJoin<TJoinRight, TKey, TJoinResult>(
        IKsqlTable<TJoinRight> rightTable,
        Expression<Func<TResult, TKey>> leftKeySelector,
        Expression<Func<TJoinRight, TKey>> rightKeySelector,
        Expression<Func<TResult, TJoinRight, TJoinResult>> resultSelector)
        where TJoinRight : class
        where TJoinResult : class
    {
        if (rightTable == null) throw new ArgumentNullException(nameof(rightTable));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result table
        string resultTableName = $"{Name}_{((KsqlTable<TJoinRight>)rightTable).Name}_leftjoin_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TJoinRight>)rightTable).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.Left,
            Name,
            ((KsqlTable<TJoinRight>)rightTable).Name,
            joinCondition);

        // Create a new table for the join result
        var result = new KsqlJoinTable<TResult, TJoinRight, TJoinResult>(
            resultTableName,
            _context,
            _schemaManager,
            this,
            (KsqlTable<TJoinRight>)rightTable,
            joinOperation,
            resultSelector);

        return result;
    }

    /// <summary>
    /// Full outer joins this table with another table.
    /// </summary>
    /// <typeparam name="TJoinRight">The type of entity in the right table.</typeparam>
    /// <typeparam name="TKey">The type of the join key.</typeparam>
    /// <typeparam name="TJoinResult">The type of the result.</typeparam>
    /// <param name="rightTable">The right table to join with.</param>
    /// <param name="leftKeySelector">A function to extract the join key from this table's elements.</param>
    /// <param name="rightKeySelector">A function to extract the join key from the right table's elements.</param>
    /// <param name="resultSelector">A function to create a result from the joined elements.</param>
    /// <returns>A table containing the joined elements.</returns>
    public IKsqlTable<TJoinResult> FullOuterJoin<TJoinRight, TKey, TJoinResult>(
        IKsqlTable<TJoinRight> rightTable,
        Expression<Func<TResult, TKey>> leftKeySelector,
        Expression<Func<TJoinRight, TKey>> rightKeySelector,
        Expression<Func<TResult, TJoinRight, TJoinResult>> resultSelector)
        where TJoinRight : class
        where TJoinResult : class
    {
        if (rightTable == null) throw new ArgumentNullException(nameof(rightTable));
        if (leftKeySelector == null) throw new ArgumentNullException(nameof(leftKeySelector));
        if (rightKeySelector == null) throw new ArgumentNullException(nameof(rightKeySelector));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        // Extract property names from key selectors
        string leftKeyProperty = ExtractPropertyName(leftKeySelector);
        string rightKeyProperty = ExtractPropertyName(rightKeySelector);

        // Create a unique name for the result table
        string resultTableName = $"{Name}_{((KsqlTable<TJoinRight>)rightTable).Name}_fullouterjoin_{Guid.NewGuid():N}";

        // Create the join condition
        string joinCondition = $"{Name}.{leftKeyProperty} = {((KsqlTable<TJoinRight>)rightTable).Name}.{rightKeyProperty}";

        // Create the join operation
        var joinOperation = new JoinOperation(
            JoinType.FullOuter,
            Name,
            ((KsqlTable<TJoinRight>)rightTable).Name,
            joinCondition);

        // Create a new table for the join result
        var result = new KsqlJoinTable<TResult, TJoinRight, TJoinResult>(
            resultTableName,
            _context,
            _schemaManager,
            this,
            (KsqlTable<TJoinRight>)rightTable,
            joinOperation,
            resultSelector);

        return result;
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
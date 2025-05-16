using System.Collections;
using System.Linq.Expressions;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;
using Ksql.EntityFramework.Schema;

namespace Ksql.EntityFramework;


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

    public string Name { get; }

    public Type ElementType => typeof(TResult);

    public Expression Expression => Expression.Constant(this);

    public IQueryProvider Provider => new KsqlQueryProvider();

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

    public async Task<TResult?> GetAsync(object key)
    {
        // In a real implementation, this would execute a KSQL query to get the entity
        // For now, we return null
        await Task.CompletedTask;
        return null;
    }

    public Task<TResult?> FindAsync(object key)
    {
        return GetAsync(key);
    }

    public Task<bool> InsertAsync(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct insertion to a join result table is not supported.");
    }

    public async Task<List<TResult>> ToListAsync()
    {
        // In a real implementation, this would execute a KSQL query to get all entities
        // For now, we return an empty list
        await Task.CompletedTask;
        return new List<TResult>();
    }

    public async IAsyncEnumerable<ChangeNotification<TResult>> ObserveChangesAsync()
    {
        // In a real implementation, this would observe changes to the join result table
        // For now, we return an empty enumerable
        await Task.CompletedTask;
        yield break;
    }

    public void Add(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct addition to a join result table is not supported.");
    }

    public void Remove(TResult entity)
    {
        // This operation is not directly supported for join results
        throw new NotSupportedException("Direct removal from a join result table is not supported.");
    }

    public IEnumerator<TResult> GetEnumerator()
    {
        // This is a placeholder implementation for enumerating a table
        // In a real implementation, this would execute a query against the table
        return Enumerable.Empty<TResult>().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

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
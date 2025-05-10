using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Extensions;

/// <summary>
/// Extension methods for KSQL queries.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Converts the query results to a list asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the query.</typeparam>
    /// <param name="source">The query to convert to a list.</param>
    /// <returns>A task representing the asynchronous operation, with the result containing the list of elements.</returns>
    public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source) where T : class
    {
        // For IKsqlTable<T> we can delegate to its own ToListAsync
        if (source is IKsqlTable<T> table)
        {
            return table.ToListAsync();
        }

        // For other IQueryable<T>, we convert to a list synchronously for simplicity
        // In a real implementation, this would be implemented asynchronously
        return Task.FromResult(source.ToList());
    }

    /// <summary>
    /// Configures error handling for the query.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the query.</typeparam>
    /// <param name="source">The query to configure error handling for.</param>
    /// <param name="errorAction">The action to take when an error occurs.</param>
    /// <returns>The query with error handling configured.</returns>
    public static IKsqlStream<T> OnError<T>(this IQueryable<T> source, ErrorAction errorAction) where T : class
    {
        // If source is already an IKsqlStream<T>, we can delegate to its OnError method
        if (source is IKsqlStream<T> stream)
        {
            return stream.OnError(errorAction);
        }

        // Otherwise, we can't apply the OnError method
        throw new InvalidOperationException("OnError can only be applied to KSQL streams.");
    }
}
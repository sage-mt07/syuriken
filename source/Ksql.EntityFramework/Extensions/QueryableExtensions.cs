using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Extensions;

public static class QueryableExtensions
{
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
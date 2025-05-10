namespace Ksql.EntityFramework.Interfaces;

/// <summary>
/// Represents a transaction for KSQL database operations.
/// </summary>
public interface IKsqlTransaction : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CommitAsync();

    /// <summary>
    /// Aborts the transaction.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AbortAsync();
}

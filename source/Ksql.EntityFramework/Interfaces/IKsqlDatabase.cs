using Ksql.EntityFramework.Configuration;

namespace Ksql.EntityFramework.Interfaces;

/// <summary>
/// Represents database-level operations for a KSQL database.
/// </summary>
public interface IKsqlDatabase
{
    /// <summary>
    /// Creates a table for the specified entity type with the given configuration options.
    /// </summary>
    /// <typeparam name="T">The type of entity for the table.</typeparam>
    /// <param name="tableName">The name of the table to create.</param>
    /// <param name="options">A function to configure the table options.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateTableAsync<T>(string tableName, Func<TableOptions, TableOptions> options) where T : class;

    /// <summary>
    /// Drops a table with the specified name.
    /// </summary>
    /// <param name="tableName">The name of the table to drop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DropTableAsync(string tableName);

    /// <summary>
    /// Drops a topic with the specified name.
    /// </summary>
    /// <param name="topicName">The name of the topic to drop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DropTopicAsync(string topicName);

    /// <summary>
    /// Executes a KSQL statement directly.
    /// </summary>
    /// <param name="ksqlStatement">The KSQL statement to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteKsqlAsync(string ksqlStatement);
}

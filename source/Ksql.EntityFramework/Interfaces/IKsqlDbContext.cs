using System;
using System.Threading.Tasks;
using Ksql.EntityFramework.Configuration;

namespace Ksql.EntityFramework.Interfaces
{
    /// <summary>
    /// Represents a KSQL database context for interacting with Kafka streams and tables.
    /// </summary>
    public interface IKsqlDbContext : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Gets the database operations for this context.
        /// </summary>
        IKsqlDatabase Database { get; }

        /// <summary>
        /// Gets the options for this context.
        /// </summary>
        KsqlDbContextOptions Options { get; }

        /// <summary>
        /// Creates a stream for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The type of entity in the stream.</typeparam>
        /// <param name="name">The name of the stream.</param>
        /// <returns>A KSQL stream.</returns>
        IKsqlStream<T> CreateStream<T>(string name) where T : class;

        /// <summary>
        /// Creates a table for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The type of entity in the table.</typeparam>
        /// <param name="name">The name of the table.</param>
        /// <returns>A KSQL table.</returns>
        IKsqlTable<T> CreateTable<T>(string name) where T : class;

        /// <summary>
        /// Creates a table for the specified entity type with a custom configuration.
        /// </summary>
        /// <typeparam name="T">The type of entity in the table.</typeparam>
        /// <param name="name">The name of the table.</param>
        /// <param name="tableBuilder">A function to configure the table.</param>
        /// <returns>A KSQL table.</returns>
        IKsqlTable<T> CreateTable<T>(string name, Func<TableBuilder<T>, TableBuilder<T>> tableBuilder) where T : class;

        /// <summary>
        /// Ensures that a topic exists for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type with Topic attribute.</typeparam>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureTopicCreatedAsync<T>() where T : class;

        /// <summary>
        /// Ensures that a stream exists for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type with Topic attribute.</typeparam>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureStreamCreatedAsync<T>() where T : class;

        /// <summary>
        /// Ensures that a table exists for the specified entity type.
        /// </summary>
        /// <param name="table">The table to ensure exists.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task EnsureTableCreatedAsync<T>(IKsqlTable<T> table) where T : class;

        /// <summary>
        /// Saves all changes made in this context to the underlying streams and tables.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SaveChangesAsync();

        /// <summary>
        /// Begins a transaction on this context.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the result containing the transaction.</returns>
        Task<IKsqlTransaction> BeginTransactionAsync();

        /// <summary>
        /// Refreshes the metadata for all streams and tables in this context.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RefreshMetadataAsync();
    }
}

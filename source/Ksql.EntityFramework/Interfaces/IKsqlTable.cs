using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Interfaces
{
    /// <summary>
    /// Represents a KSQL table that can be queried and updated.
    /// </summary>
    /// <typeparam name="T">The type of entity in the table.</typeparam>
    public interface IKsqlTable<T> : IQueryable<T> where T : class
    {
        /// <summary>
        /// Gets an entity from the table by its key.
        /// </summary>
        /// <param name="key">The primary key of the entity.</param>
        /// <returns>A task representing the asynchronous operation, with the result containing the entity or null if not found.</returns>
        Task<T?> GetAsync(object key);

        /// <summary>
        /// Finds an entity from the table by its key.
        /// </summary>
        /// <param name="key">The primary key of the entity.</param>
        /// <returns>A task representing the asynchronous operation, with the result containing the entity or null if not found.</returns>
        Task<T?> FindAsync(object key);

        /// <summary>
        /// Inserts an entity into the table.
        /// </summary>
        /// <param name="entity">The entity to insert.</param>
        /// <returns>A task representing the asynchronous operation, with the result indicating whether the insert was successful.</returns>
        Task<bool> InsertAsync(T entity);

        /// <summary>
        /// Retrieves all entities from the table as a list.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the result containing the list of entities.</returns>
        Task<List<T>> ToListAsync();

        /// <summary>
        /// Observes changes to the table and receives change notifications.
        /// </summary>
        /// <returns>An asynchronous enumerable of change notifications.</returns>
        IAsyncEnumerable<ChangeNotification<T>> ObserveChangesAsync();

        /// <summary>
        /// Adds a table entity to be saved when SaveChanges is called.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        void Add(T entity);

        /// <summary>
        /// Removes a table entity to be deleted when SaveChanges is called.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        void Remove(T entity);
    }
}

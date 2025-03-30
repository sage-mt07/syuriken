using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Schema;

namespace Ksql.EntityFramework
{
    /// <summary>
    /// Base class for KSQL database contexts.
    /// </summary>
    public abstract class KsqlDbContext : IKsqlDbContext
    {
        private readonly Dictionary<Type, object> _streams = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _tables = new Dictionary<Type, object>();
        private readonly List<object> _pendingChanges = new List<object>();
        private readonly SchemaManager _schemaManager;
        private readonly KsqlDatabase _database;
        private bool _disposed;

        /// <summary>
        /// Gets the database operations for this context.
        /// </summary>
        public IKsqlDatabase Database => _database;

        /// <summary>
        /// Gets the options for this context.
        /// </summary>
        public KsqlDbContextOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KsqlDbContext"/> class.
        /// </summary>
        protected KsqlDbContext() : this(new KsqlDbContextOptions())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KsqlDbContext"/> class with the specified options.
        /// </summary>
        /// <param name="options">The options for the context.</param>
        protected KsqlDbContext(KsqlDbContextOptions options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            _schemaManager = new SchemaManager(options);
            _database = new KsqlDatabase(options, _schemaManager);
            InitializeContext();
        }

        /// <summary>
        /// Creates a stream for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The type of entity in the stream.</typeparam>
        /// <param name="name">The name of the stream.</param>
        /// <returns>A KSQL stream.</returns>
        public IKsqlStream<T> CreateStream<T>(string name) where T : class
        {
            var stream = new KsqlStream<T>(name, this, _schemaManager);
            _streams[typeof(T)] = stream;
            return stream;
        }

        /// <summary>
        /// Creates a table for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The type of entity in the table.</typeparam>
        /// <param name="name">The name of the table.</param>
        /// <returns>A KSQL table.</returns>
        public IKsqlTable<T> CreateTable<T>(string name) where T : class
        {
            var table = new KsqlTable<T>(name, this, _schemaManager);
            _tables[typeof(T)] = table;
            return table;
        }

        /// <summary>
        /// Creates a table for the specified entity type with a custom configuration.
        /// </summary>
        /// <typeparam name="T">The type of entity in the table.</typeparam>
        /// <param name="name">The name of the table.</param>
        /// <param name="tableBuilder">A function to configure the table.</param>
        /// <returns>A KSQL table.</returns>
        public IKsqlTable<T> CreateTable<T>(string name, Func<TableBuilder<T>, TableBuilder<T>> tableBuilder) where T : class
        {
            var builder = new TableBuilder<T>(name);
            builder = tableBuilder(builder);
            var source = builder.GetSource();
            var options = builder.Build();

            var table = new KsqlTable<T>(name, this, _schemaManager, options);
            _tables[typeof(T)] = table;
            return table;
        }

        /// <summary>
        /// Ensures that a topic exists for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type with Topic attribute.</typeparam>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureTopicCreatedAsync<T>() where T : class
        {
            var topicDescriptor = _schemaManager.GetTopicDescriptor<T>();
            await _database.EnsureTopicCreatedAsync(topicDescriptor).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures that a stream exists for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type with Topic attribute.</typeparam>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureStreamCreatedAsync<T>() where T : class
        {
            var topicDescriptor = _schemaManager.GetTopicDescriptor<T>();
            await _database.EnsureStreamCreatedAsync(topicDescriptor).ConfigureAwait(false);
        }

        /// <summary>
        /// Ensures that a table exists for the specified entity type.
        /// </summary>
        /// <param name="table">The table to ensure exists.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task EnsureTableCreatedAsync<T>(IKsqlTable<T> table) where T : class
        {
            if (table == null) throw new ArgumentNullException(nameof(table));

            var ksqlTable = table as KsqlTable<T> ?? throw new ArgumentException("The table must be created by this context.", nameof(table));
            await _database.EnsureTableCreatedAsync(ksqlTable.GetTableDescriptor()).ConfigureAwait(false);
        }

        /// <summary>
        /// Saves all changes made in this context to the underlying streams and tables.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SaveChangesAsync()
        {
            foreach (var change in _pendingChanges)
            {
                await SaveChangeAsync(change).ConfigureAwait(false);
            }

            _pendingChanges.Clear();
        }

        /// <summary>
        /// Begins a transaction on this context.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the result containing the transaction.</returns>
        public async Task<IKsqlTransaction> BeginTransactionAsync()
        {
            return await _database.BeginTransactionAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Refreshes the metadata for all streams and tables in this context.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task RefreshMetadataAsync()
        {
            await _database.RefreshMetadataAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Adds an entity to the pending changes list.
        /// </summary>
        /// <typeparam name="T">The type of entity to add.</typeparam>
        /// <param name="entity">The entity to add.</param>
        internal void AddToPendingChanges<T>(T entity) where T : class
        {
            _pendingChanges.Add(entity);
        }

        private void InitializeContext()
        {
            // Initialize the context by setting up the streams and tables
            // based on properties in the derived class

            var properties = GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsGenericType)
                {
                    var genericType = property.PropertyType.GetGenericTypeDefinition();
                    var entityType = property.PropertyType.GetGenericArguments()[0];

                    if (genericType == typeof(IKsqlStream<>))
                    {
                        var createStreamMethod = typeof(KsqlDbContext).GetMethod(nameof(CreateStream))?.MakeGenericMethod(entityType);
                        var stream = createStreamMethod?.Invoke(this, new object[] { entityType.Name.ToLowerInvariant() });
                        property.SetValue(this, stream);
                    }
                    else if (genericType == typeof(IKsqlTable<>))
                    {
                        var createTableMethod = typeof(KsqlDbContext).GetMethod(nameof(CreateTable), new[] { typeof(string) })?.MakeGenericMethod(entityType);
                        var table = createTableMethod?.Invoke(this, new object[] { entityType.Name.ToLowerInvariant() });
                        property.SetValue(this, table);
                    }
                }
            }
        }

        private async Task SaveChangeAsync(object change)
        {
            // Determine the type of change and save it to the appropriate stream or table
            var changeType = change.GetType();
            
            if (_streams.TryGetValue(changeType, out var streamObj))
            {
                var streamType = typeof(KsqlStream<>).MakeGenericType(changeType);
                var produceAsyncMethod = streamType.GetMethod("ProduceAsync", new[] { changeType });
                await (Task)produceAsyncMethod.Invoke(streamObj, new[] { change });
            }
            else if (_tables.TryGetValue(changeType, out var tableObj))
            {
                var tableType = typeof(KsqlTable<>).MakeGenericType(changeType);
                var insertAsyncMethod = tableType.GetMethod("InsertAsync", new[] { changeType });
                await (Task)insertAsyncMethod.Invoke(tableObj, new[] { change });
            }
            else
            {
                throw new InvalidOperationException($"No stream or table found for entity type {changeType.Name}");
            }
        }

        /// <summary>
        /// Disposes the context.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the context asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            Dispose(false);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the context.
        /// </summary>
        /// <param name="disposing">Whether the method is being called from Dispose.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed state (managed objects)
                    _database.Dispose();
                }

                // Free unmanaged resources (unmanaged objects) and override finalizer
                // Set large fields to null
                _disposed = true;
            }
        }

        /// <summary>
        /// Disposes the context asynchronously.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_database is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _database.Dispose();
            }
        }
    }
}

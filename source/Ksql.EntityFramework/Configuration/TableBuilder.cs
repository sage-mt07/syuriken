using System;
using System.Linq.Expressions;
using Ksql.EntityFramework.Interfaces;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Configuration
{
    /// <summary>
    /// Builder for configuring KSQL tables.
    /// </summary>
    /// <typeparam name="T">The entity type for the table.</typeparam>
    public class TableBuilder<T> where T : class
    {
        private readonly TableOptions _options = new TableOptions();
        private string? _streamSource;
        private string? _topicSource;

        /// <summary>
        /// Gets the name of the table being built.
        /// </summary>
        public string TableName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TableBuilder{T}"/> class with the specified table name.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        public TableBuilder(string tableName)
        {
            TableName = tableName ?? throw new ArgumentNullException(nameof(tableName));
        }

        /// <summary>
        /// Configures the table to be created from a KSQL stream.
        /// </summary>
        /// <param name="stream">The stream to create the table from.</param>
        /// <returns>The table builder for method chaining.</returns>
        public TableBuilder<T> FromStream<TStream>(IKsqlStream<TStream> stream) where TStream : class
        {
            _streamSource = typeof(TStream).Name.ToLowerInvariant();
            _topicSource = null;
            return this;
        }

        /// <summary>
        /// Configures the table to be created from a Kafka topic.
        /// </summary>
        /// <typeparam name="TSource">The type of entity in the topic.</typeparam>
        /// <param name="topicName">The name of the topic.</param>
        /// <returns>The table builder for method chaining.</returns>
        public TableBuilder<T> FromTopic<TSource>(string topicName) where TSource : class
        {
            _topicSource = topicName;
            _streamSource = null;
            _options.WithTopic(topicName);
            return this;
        }

        /// <summary>
        /// Specifies the key columns for the table.
        /// </summary>
        /// <param name="keySelector">An expression to select the key property.</param>
        /// <returns>The table builder for method chaining.</returns>
        public TableBuilder<T> WithKeyColumn<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            var propertyName = ExtractPropertyName(keySelector);
            _options.KeyColumns.Add(propertyName);
            return this;
        }

        /// <summary>
        /// Specifies the format to use for values in the table.
        /// </summary>
        /// <param name="format">The value format.</param>
        /// <returns>The table builder for method chaining.</returns>
        public TableBuilder<T> WithValueFormat(ValueFormat format)
        {
            _options.ValueFormat = format;
            return this;
        }

        /// <summary>
        /// Specifies the timestamp column and format for the table.
        /// </summary>
        /// <param name="timestampSelector">An expression to select the timestamp property.</param>
        /// <param name="format">The format of the timestamp.</param>
        /// <returns>The table builder for method chaining.</returns>
        public TableBuilder<T> WithTimestamp<TTimestamp>(Expression<Func<T, TTimestamp>> timestampSelector, string? format = null)
        {
            var propertyName = ExtractPropertyName(timestampSelector);
            _options.TimestampColumn = propertyName;
            _options.TimestampFormat = format;
            return this;
        }

        /// <summary>
        /// Builds the configuration for the table.
        /// </summary>
        /// <returns>The table options.</returns>
        public TableOptions Build()
        {
            return _options;
        }

        /// <summary>
        /// Gets the source information for the table.
        /// </summary>
        /// <returns>A tuple containing the stream source and topic source.</returns>
        public (string? StreamSource, string? TopicSource) GetSource()
        {
            return (_streamSource, _topicSource);
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
}

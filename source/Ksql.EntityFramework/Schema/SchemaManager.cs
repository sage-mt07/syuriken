using System;
using System.Collections.Generic;
using System.Reflection;
using Ksql.EntityFramework.Attributes;
using Ksql.EntityFramework.Configuration;
using Ksql.EntityFramework.Models;

namespace Ksql.EntityFramework.Schema
{
    /// <summary>
    /// Manages schema information for entity types in the KSQL database.
    /// </summary>
    internal class SchemaManager
    {
        private readonly KsqlDbContextOptions _options;
        private readonly Dictionary<Type, TopicDescriptor> _topicDescriptors = new Dictionary<Type, TopicDescriptor>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SchemaManager"/> class.
        /// </summary>
        /// <param name="options">The options for the database context.</param>
        public SchemaManager(KsqlDbContextOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets a topic descriptor for the specified entity type.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>A topic descriptor for the entity type.</returns>
        public TopicDescriptor GetTopicDescriptor<T>() where T : class
        {
            var entityType = typeof(T);
            
            if (!_topicDescriptors.TryGetValue(entityType, out var descriptor))
            {
                descriptor = CreateTopicDescriptor(entityType);
                _topicDescriptors[entityType] = descriptor;
            }

            return descriptor;
        }

        /// <summary>
        /// Gets a schema string for the specified entity type.
        /// </summary>
        /// <param name="entityType">The entity type.</param>
        /// <returns>A schema string for the entity type.</returns>
        public string GetSchemaString(Type entityType)
        {
            var properties = entityType.GetProperties();
            var schemaEntries = new List<string>();

            foreach (var property in properties)
            {
                var schemaType = GetSchemaType(property);
                schemaEntries.Add($"  {property.Name} {schemaType}");
            }

            return string.Join(",\n", schemaEntries);
        }

        private TopicDescriptor CreateTopicDescriptor(Type entityType)
        {
            var topicAttribute = entityType.GetCustomAttribute<TopicAttribute>();
            var topicName = topicAttribute?.Name ?? entityType.Name.ToLowerInvariant();
            var partitionCount = topicAttribute?.PartitionCount ?? _options.DefaultPartitionCount;
            var replicationFactor = topicAttribute?.ReplicationFactor ?? _options.DefaultReplicationFactor;

            string? keyColumn = null;
            string? timestampColumn = null;
            string? timestampFormat = null;

            var properties = entityType.GetProperties();
            foreach (var property in properties)
            {
                if (property.GetCustomAttribute<KeyAttribute>() != null)
                {
                    keyColumn = property.Name;
                }

                var timestampAttribute = property.GetCustomAttribute<TimestampAttribute>();
                if (timestampAttribute != null)
                {
                    timestampColumn = property.Name;
                    timestampFormat = timestampAttribute.Format;
                }
            }

            return new TopicDescriptor
            {
                Name = topicName,
                EntityType = entityType,
                PartitionCount = partitionCount,
                ReplicationFactor = replicationFactor,
                KeyColumn = keyColumn,
                TimestampColumn = timestampColumn,
                TimestampFormat = timestampFormat,
                ValueFormat = _options.DefaultValueFormat
            };
        }

        private string GetSchemaType(PropertyInfo property)
        {
            var type = property.PropertyType;
            var isNullable = Nullable.GetUnderlyingType(type) != null;

            if (isNullable)
            {
                type = Nullable.GetUnderlyingType(type)!;
            }

            var schemaType = MapType(type);

            return isNullable ? schemaType : schemaType;
        }

        private string MapType(Type type)
        {
            if (type == typeof(bool))
            {
                return "BOOLEAN";
            }
            else if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort))
            {
                return "SMALLINT";
            }
            else if (type == typeof(int) || type == typeof(uint))
            {
                return "INTEGER";
            }
            else if (type == typeof(long) || type == typeof(ulong))
            {
                return "BIGINT";
            }
            else if (type == typeof(float))
            {
                return "REAL";
            }
            else if (type == typeof(double))
            {
                return "DOUBLE";
            }
            else if (type == typeof(decimal))
            {
                return "DECIMAL(18, 8)";
            }
            else if (type == typeof(string))
            {
                return "VARCHAR";
            }
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                return "TIMESTAMP";
            }
            else if (type == typeof(TimeSpan))
            {
                return "TIME";
            }
            else if (type == typeof(Guid))
            {
                return "VARCHAR";
            }
            else if (type.IsArray)
            {
                var elementType = type.GetElementType()!;
                var elementSchemaType = MapType(elementType);
                return $"ARRAY<{elementSchemaType}>";
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = type.GetGenericArguments()[0];
                var elementSchemaType = MapType(elementType);
                return $"ARRAY<{elementSchemaType}>";
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var keyType = type.GetGenericArguments()[0];
                var valueType = type.GetGenericArguments()[1];

                if (keyType != typeof(string))
                {
                    throw new NotSupportedException("Only string keys are supported for dictionaries in KSQL.");
                }

                var valueSchemaType = MapType(valueType);
                return $"MAP<VARCHAR, {valueSchemaType}>";
            }
            else
            {
                // For complex types, we use VARCHAR and serialize to JSON
                return "VARCHAR";
            }
        }
    }
}

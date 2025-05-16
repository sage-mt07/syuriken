using System.Reflection;
using Ksql.EntityFramework.Attributes;
using Ksql.EntityFramework.Configuration;

namespace Ksql.EntityFramework.Schema;

public class SchemaManager
{
    private readonly KsqlDbContextOptions _options;
    private readonly Dictionary<Type, TopicDescriptor> _topicDescriptors = new Dictionary<Type, TopicDescriptor>();

    public SchemaManager(KsqlDbContextOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

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
    public IReadOnlyList<PropertyInfo> GetKeyProperties(Type entityType)
    {
        // Key属性を持つプロパティを取得し、Orderでソート
        return entityType.GetProperties()
            .Where(p => p.GetCustomAttribute<KeyAttribute>() != null)
            .OrderBy(p => p.GetCustomAttribute<KeyAttribute>().Order)
            .ToList();
    }
    public string[] GetKeyPropertyNames(Type entityType)
    {
        return GetKeyProperties(entityType).Select(p => p.Name).ToArray();
    }
    public string CreateKeyString<T>(T entity) where T : class
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));

        var keyProperties = GetKeyProperties(typeof(T));
        if (keyProperties.Count == 0)
            throw new InvalidOperationException($"No key properties found for type '{typeof(T).Name}'.");

        // 単一キーの場合は単純な文字列として返す
        if (keyProperties.Count == 1)
        {
            var value = keyProperties[0].GetValue(entity);
            return value?.ToString() ?? string.Empty;
        }

        // 複合キーの場合はキー部分をJOINして返す
        var keyParts = keyProperties.Select(p => p.GetValue(entity)?.ToString() ?? string.Empty);
        return string.Join("|", keyParts);
    }
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

        // 複合キーを含む可能性のあるキー列を取得
        var keyColumns = GetKeyPropertyNames(entityType);

        string? timestampColumn = null;
        string? timestampFormat = null;

        var properties = entityType.GetProperties();
        foreach (var property in properties)
        {
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
            KeyColumns = keyColumns.ToList(),
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

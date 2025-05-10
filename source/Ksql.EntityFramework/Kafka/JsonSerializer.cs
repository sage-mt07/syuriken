using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Ksql.EntityFramework.Kafka;

/// <summary>
/// Serializes objects to JSON for Kafka.
/// </summary>
/// <typeparam name="T">The type of object to serialize.</typeparam>
internal class JsonSerializer<T> : ISerializer<T>
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSerializer{T}"/> class.
    /// </summary>
    /// <param name="options">The JSON serializer options.</param>
    public JsonSerializer(JsonSerializerOptions options = null)
    {
        _options = options ?? new JsonSerializerOptions();
    }

    /// <summary>
    /// Serializes the specified object to a byte array.
    /// </summary>
    /// <param name="data">The object to serialize.</param>
    /// <param name="context">The serialization context.</param>
    /// <returns>The serialized object as a byte array.</returns>
    public byte[] Serialize(T data, SerializationContext context)
    {
        if (data == null)
        {
            return null;
        }

        var json = JsonSerializer.Serialize(data, _options);
        return Encoding.UTF8.GetBytes(json);
    }
}

/// <summary>
/// Deserializes JSON to objects for Kafka.
/// </summary>
/// <typeparam name="T">The type of object to deserialize.</typeparam>
internal class JsonDeserializer<T> : IDeserializer<T>
{
    private readonly JsonSerializerOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonDeserializer{T}"/> class.
    /// </summary>
    /// <param name="options">The JSON serializer options.</param>
    public JsonDeserializer(JsonSerializerOptions options = null)
    {
        _options = options ?? new JsonSerializerOptions();
    }

    /// <summary>
    /// Deserializes the specified byte array to an object.
    /// </summary>
    /// <param name="data">The byte array to deserialize.</param>
    /// <param name="isNull">Whether the data is null.</param>
    /// <param name="context">The deserialization context.</param>
    /// <returns>The deserialized object.</returns>
    public T Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        if (isNull || data.Length == 0)
        {
            return default;
        }

        var json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<T>(json, _options);
    }
}
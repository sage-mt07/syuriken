using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Ksql.EntityFramework.Kafka;

internal class JsonSerializer<T> : ISerializer<T>
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializer(JsonSerializerOptions options = null)
    {
        _options = options ?? new JsonSerializerOptions();
    }

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

internal class JsonDeserializer<T> : IDeserializer<T>
{
    private readonly JsonSerializerOptions _options;

    public JsonDeserializer(JsonSerializerOptions options = null)
    {
        _options = options ?? new JsonSerializerOptions();
    }

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
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using Foster.Framework;
using Foster.Framework.JsonConverters;

namespace Moggy.Assets;

public sealed class JsonAsset<T> : AssetResource
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        Converters =
        {
            new JsonStringEnumConverter(),
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new Matrix3x2Converter(),
            new IsoTimeSpanConverter()
        }
    };

    public T Value { get; private set; } = default!;

    public override void Load(AssetLoadContext context, Stream stream)
    {
        Value = JsonSerializer.Deserialize<T>(stream, _jsonOptions)
                ?? throw new InvalidOperationException($"JSON asset '{Name}' could not be deserialized.");
    }
}

internal class IsoTimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value)) return TimeSpan.Zero;
        return XmlConvert.ToTimeSpan(value);
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(XmlConvert.ToString(value));
    }
}

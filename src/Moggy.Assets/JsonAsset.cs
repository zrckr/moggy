using System.Text.Json;
using System.Text.Json.Serialization;
using Foster.Framework;
using Foster.Framework.JsonConverters;

namespace Moggy.Assets;

public sealed class JsonAsset<T> : AssetResource
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter(),
            new Vector2Converter(),
            new Vector3Converter(),
            new Vector4Converter(),
            new Matrix3x2Converter()
        }
    };

    public T Value { get; private set; } = default!;

    public override void Load(AssetLoadContext context, Stream stream)
    {
        Value = JsonSerializer.Deserialize<T>(stream, _jsonOptions)
                ?? throw new InvalidOperationException($"JSON asset '{Name}' could not be deserialized.");
    }
}

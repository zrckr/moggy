using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moggy.Assets;

public abstract class AssetProvider : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public abstract Stream LoadStream(string path);

    public T LoadJson<T>(string path)
    {
        if (Path.GetExtension(path) != ".json")
        {
            throw new ArgumentException("The asset path must end with '.json'.");
        }

        using var stream = LoadStream(path);
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)!;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim();
        if (Path.HasExtension(normalized))
        {
            normalized = Path.ChangeExtension(normalized, null);
        }

        return normalized.Trim('/').ToLowerInvariant();
    }
}
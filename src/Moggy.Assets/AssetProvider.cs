using System.Text.Json;
using System.Text.Json.Serialization;

namespace Moggy.Assets;

public abstract class AssetProvider : IDisposable
{
    private const string JsonExtension = ".json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public abstract Stream LoadStream(string path);

    private bool TryLoadStream(string path, out Stream? stream)
    {
        try
        {
            stream = LoadStream(path);
            return true;
        }
        catch (FileNotFoundException)
        {
            stream = null;
            return false;
        }
    }

    public T LoadJson<T>(string path)
    {
        if (!Path.GetExtension(path).Equals(JsonExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The asset path must end with '{JsonExtension}'.", nameof(path));
        }

        using var stream = LoadStream(path);
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)!;
    }

    public bool TryLoadJson<T>(string path, out T value)
    {
        if (!Path.GetExtension(path).Equals(JsonExtension, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"The asset path must end with '{JsonExtension}'.", nameof(path));
        }

        if (!TryLoadStream(path, out var stream) || stream == null)
        {
            value = default!;
            return false;
        }

        using (stream)
        {
            value = JsonSerializer.Deserialize<T>(stream, JsonOptions)
                    ?? throw new InvalidOperationException($"JSON asset '{path}' could not be deserialized.");
        }

        return true;
    }

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    protected static bool PathMatches(string requestedPath, string candidatePath)
    {
        var normalizedRequest = NormalizePath(requestedPath);
        var normalizedCandidate = NormalizePath(candidatePath);
        if (Path.HasExtension(normalizedRequest))
        {
            return normalizedRequest.Equals(normalizedCandidate, StringComparison.OrdinalIgnoreCase);
        }

        // Sidecar JSON files must be requested with their extension
        if (Path.GetExtension(normalizedCandidate).Equals(JsonExtension, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var candidateWithoutExtension = Path.ChangeExtension(normalizedCandidate, null);
        return normalizedRequest.Equals(candidateWithoutExtension, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Replace('\\', '/').Trim();
        return normalized.Trim('/').ToLowerInvariant();
    }
}

using Foster.Framework;

namespace Moggy.Assets;

public class AssetLoader : IDisposable
{
    private const string Root = "Content";

    private const string Bundle = "ContentBundle";

    private readonly App _app;

    private readonly AssetProvider _provider;

    private readonly List<Type> _types = new();

    private readonly List<Entry> _entries = new();

    private ulong _nextId = 1;

    public AssetLoader(App app)
    {
        _app = app;
#if DEBUG
        _provider = new DirectoryProvider(Root);
#else
        var assembly = app.GetType().Assembly;
        var stream = assembly.GetManifestResourceStream(Bundle)
            ?? throw new FileNotFoundException($"Embedded content resource not found: {Bundle}!");
        _provider = new ZipProvider(stream);
#endif
        _types.Add(typeof(SpriteAsset));
        _types.Add(typeof(ImageAsset));
    }

    public T Load<T>(string path) where T : AssetResource, new()
    {
        return Load<T>(path, requireRegistration: true, normalizePath: null);
    }

    public T LoadJson<T>(string path)
    {
        return Load<JsonAsset<T>>(path, requireRegistration: false, normalizePath: EnsureJsonPath).Value;
    }

    public bool TryGet<T>(AssetId id, out T? asset) where T : AssetResource
    {
        PruneCollected();

        if (TryFind(id, out var entry) &&
            entry.Asset.TryGetTarget(out var existingAsset) &&
            existingAsset is T { IsDisposing: false } typed)
        {
            asset = typed;
            return true;
        }

        asset = null;
        return false;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var entry in _entries.ToArray())
        {
            if (entry.Asset.TryGetTarget(out var asset))
            {
                asset.Dispose();
            }
        }

        _entries.Clear();
        _types.Clear();
        _provider.Dispose();
    }

    private T Load<T>(string path, bool requireRegistration, Func<string, string>? normalizePath) where T : AssetResource, new()
    {
        PruneCollected();
        path = normalizePath?.Invoke(path) ?? path;

        if (TryFind(path, out var entry) &&
            entry.Asset.TryGetTarget(out var existingAsset) &&
            existingAsset is T { IsDisposing: false } typedAsset)
        {
            return typedAsset;
        }

        if (requireRegistration && _types.All(type => type != typeof(T)))
        {
            throw new NotSupportedException($"No asset loader is registered for '{path}'.");
        }

        using var stream = _provider.LoadStream(path);
        var id = NextId();
        var asset = new T
        {
            Id = id,
            Name = path
        };
        asset.Load(_app, stream);

        _entries.Add(new Entry
        {
            Path = path,
            Id = id,
            Asset = new WeakReference<AssetResource>(asset)
        });

        return asset;
    }

    private AssetId NextId()
    {
        if (_nextId == 0)
        {
            throw new InvalidOperationException("Asset id space exhausted.");
        }

        return new AssetId(_nextId++);
    }

    private static string EnsureJsonPath(string path)
    {
        return Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase)
            ? path
            : Path.ChangeExtension(path, ".json");
    }

    private bool TryFind(string path, out Entry entry)
    {
        foreach (var asset in _entries)
        {
            if (string.Equals(asset.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                entry = asset;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    private bool TryFind(AssetId id, out Entry entry)
    {
        foreach (var asset in _entries)
        {
            if (asset.Id == id)
            {
                entry = asset;
                return true;
            }
        }

        entry = null!;
        return false;
    }

    private void PruneCollected()
    {
        for (var i = _entries.Count - 1; i >= 0; i--)
        {
            if (!_entries[i].Asset.TryGetTarget(out var asset) || asset.IsDisposing)
            {
                _entries.RemoveAt(i);
            }
        }
    }

    private sealed class Entry
    {
        public required string Path { get; init; }

        public required AssetId Id { get; init; }

        public required WeakReference<AssetResource> Asset { get; init; }
    }
}

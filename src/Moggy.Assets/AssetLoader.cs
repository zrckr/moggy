using Foster.Framework;

namespace Moggy.Assets;

public class AssetLoader : IDisposable
{
    private const int UnloadAfterIdleFrames = 300;

    private const string Root = "Content";

    private const string Bundle = "ContentBundle";

    private readonly App _app;

    private readonly AssetProvider _provider;

    private readonly List<Type> _types = new();

    private readonly Dictionary<AssetId, AssetResource> _assets = new();

    private readonly Dictionary<AssetId, ulong> _lastTouched = new();

    private ulong _frame;

    public AssetLoader(App app)
    {
        _app = app;
#if DEBUG
        _provider = new DirectoryProvider(Root);
#else
        var assembly = typeof(App).Assembly;
        var stream = assembly.GetManifestResourceStream(Bundle)
            ?? throw new FileNotFoundException($"Embedded content resource not found: {Bundle}!");
        _provider = new ZipAssetProvider();
#endif
    }

    public void Register<T>() where T : AssetResource
    {
        var type = typeof(T);
        if (_types.Contains(type))
        {
            throw new ArgumentException("An asset type is already registered.", type.Name);
        }

        _types.Add(type);
    }

    public AssetId Load<T>(string path, out T? asset) where T : AssetResource, new()
    {
        var id = new AssetId(Fnv1A64(path));
        if (_assets.TryGetValue(id, out var existingAsset))
        {
            _lastTouched[id] = _frame;
            asset = existingAsset as T;
            return id;
        }

        if (_types.All(type => type != typeof(T)))
        {
            throw new NotSupportedException($"No asset loader is registered for '{path}'.");
        }

        using var stream = _provider.LoadStream(path);
        asset = new T();
        asset.Name = path;
        asset.Load(_app, stream);
        _assets.Add(id, asset);
        _lastTouched[id] = _frame;
        return id;
    }

    public void Check()
    {
        _frame++;
        foreach (var (id, asset) in _assets.ToArray())
        {
            if (_lastTouched.TryGetValue(id, out var lastTouched) &&
                _frame - lastTouched <= UnloadAfterIdleFrames)
            {
                continue;
            }

            _assets.Remove(id);
            _lastTouched.Remove(id);
            asset.Dispose();
        }
    }

    public T Get<T>(AssetId id) where T : AssetResource
    {
        if (!_assets.TryGetValue(id, out var asset))
        {
            throw new KeyNotFoundException($"Asset '{id.Id}' is not loaded.");
        }

        if (asset is not T typed)
        {
            throw new InvalidCastException(
                $"Asset '{id.Id}' is '{asset.GetType().Name}', not '{typeof(T).Name}'.");
        }

        return typed;
    }

    public bool TryGet<T>(AssetId id, out T? asset) where T : AssetResource
    {
        var exists = _assets.TryGetValue(id, out var existingAsset);
        asset = existingAsset as T;
        return exists;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var asset in _assets.Values)
        {
            asset.Dispose();
        }

        _assets.Clear();
        _lastTouched.Clear();
        _types.Clear();
        _provider.Dispose();
    }

    private static ulong Fnv1A64(string value)
    {
        const ulong offset = 0xCBF29CE484222325UL;
        const ulong prime = 0x00000100000001B3UL;

        var hash = offset;
        foreach (var character in value)
        {
            unchecked
            {
                hash ^= character;
                hash *= prime;
            }
        }

        return hash;
    }
}
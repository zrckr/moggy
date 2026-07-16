using Foster.Framework;

namespace Moggy.Assets;

public sealed class AssetLoadContext
{
    public App App { get; }

    public string AssetPath { get; }

    private readonly AssetProvider _provider;

    internal AssetLoadContext(App app, AssetProvider provider, string assetPath)
    {
        App = app;
        AssetPath = assetPath;
        _provider = provider;
    }

    public bool TryLoadMetadata<T>(out T metadata)
    {
        var metadataPath = Path.ChangeExtension(AssetPath, ".json");
        return _provider.TryLoadJson(metadataPath, out metadata);
    }
}
using Foster.Framework;

namespace Moggy.Assets;

public abstract class AssetResource : IDisposable
{
    public string Name { get; internal set; } = "";

    public abstract void Load(App app, Stream stream);

    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
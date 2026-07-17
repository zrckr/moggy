namespace Moggy.Assets;

public abstract class AssetResource : IDisposable
{
    public AssetId Id { get; internal init; } = AssetId.Invalid;

    public string Name { get; internal init; } = "";

    public bool IsDisposing { get; private set; }

    public event Action? Disposed;

    public abstract void Load(AssetLoadContext context, Stream stream);

    public virtual void Dispose()
    {
        if (!IsDisposing)
        {
            IsDisposing = true;
            GC.SuppressFinalize(this);
            Disposed?.Invoke();
        }
    }

    public static implicit operator AssetId(AssetResource resource)
    {
        return resource.Id;
    }
}

using Foster.Framework;

namespace Moggy.Assets;

public class ImageAsset : AssetResource
{
    public int Width { get; private set; }

    public int Height { get; private set; }

    public RectInt Bounds { get; private set; }

    public Texture Texture { get; private set; } = null!;

    public override void Load(App app, Stream stream)
    {
        using var image = new Image(stream);
        if (image.Width == 0 || image.Height == 0)
        {
            throw new InvalidOperationException($"Image '{Name}' has invalid size.");
        }

        Width = image.Width;
        Height = image.Height;
        Bounds = image.Bounds;
        Texture = new Texture(app.GraphicsDevice, image, Name);
    }

    public override void Dispose()
    {
        if (IsDisposing)
        {
            return;
        }

        base.Dispose();
        Texture.Dispose();
    }
}
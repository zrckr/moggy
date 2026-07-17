using System.Text.Json.Serialization;
using Foster.Framework;

namespace Moggy.Assets;

public sealed class FontAsset : AssetResource
{
    [JsonInclude] public int Size { get; private set; } = 16;

    [JsonInclude] public bool PixelPerfect { get; private set; }

    [JsonInclude] public TextureFilter Filter { get; private set; } = TextureFilter.Linear;

    [JsonInclude] public TextureWrap Wrap { get; private set; } = TextureWrap.Clamp;

    [JsonIgnore] public SpriteFont Sprite { get; private set; } = null!;

    private Font _font = null!;

    private GraphicsDevice _graphicsDevice = null!;

    public override void Load(AssetLoadContext context, Stream stream)
    {
        if (context.TryLoadMetadata<FontAsset>(out var metadata))
        {
            Size = metadata.Size;
            PixelPerfect = metadata.PixelPerfect;
            Filter = metadata.Filter;
            Wrap = metadata.Wrap;
        }

        _font = new Font(stream);
        _graphicsDevice = context.App.GraphicsDevice;
        Sprite = new SpriteFont(_graphicsDevice, _font, Size, pixelPerfect: PixelPerfect)
        {
            Sampler = new TextureSampler(Filter, Wrap)
        };
    }

    public override void Dispose()
    {
        if (!IsDisposing)
        {
            base.Dispose();
            Sprite.Dispose();
            _font.Dispose();
        }
    }
}

using System.Numerics;
using Foster.Framework;

namespace Moggy;

public struct Viewport(int virtualWidth, int virtualHeight, RectInt contentBounds)
{
    public Vector2 Origin => new(VirtualWidth * 0.5f, VirtualHeight * 0.5f);

    public readonly int VirtualWidth = virtualWidth;

    public readonly int VirtualHeight = virtualHeight;

    public readonly RectInt ContentBounds = contentBounds;

    public RectInt WindowBounds;

    public float Scale = 1f;
}

public sealed class ViewportSystem : GameSystem
{
    public override void Render(Time time)
    {
        ref var viewport = ref Registry.Singleton<Viewport>();
        viewport.Scale = MathF.Floor(MathF.Min(
            Game.Window.WidthInPixels / (float)viewport.VirtualWidth,
            Game.Window.HeightInPixels / (float)viewport.VirtualHeight));
        viewport.Scale = MathF.Max(1, viewport.Scale);

        var width = (int)(viewport.VirtualWidth * viewport.Scale);
        var height = (int)(viewport.VirtualHeight * viewport.Scale);
        viewport.WindowBounds = new RectInt(
            (Game.Window.WidthInPixels - width) / 2,
            (Game.Window.HeightInPixels - height) / 2,
            width,
            height);
    }
}

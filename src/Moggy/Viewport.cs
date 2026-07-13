using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

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
    private Query _viewport = null!;

    public override void Startup()
    {
        _viewport = Registry.Query()
            .Include<Viewport>()
            .Build();
    }

    public override void Update(Time time)
    {
        var entity = _viewport.Single();

        ref var viewport = ref Registry.Get<Viewport>(entity);
        viewport.Scale = MathF.Floor(MathF.Min(
            App.Window.WidthInPixels / (float)viewport.VirtualWidth,
            App.Window.HeightInPixels / (float)viewport.VirtualHeight));
        viewport.Scale = MathF.Max(1, viewport.Scale);

        var width = (int)(viewport.VirtualWidth * viewport.Scale);
        var height = (int)(viewport.VirtualHeight * viewport.Scale);
        viewport.WindowBounds = new RectInt(
            (App.Window.WidthInPixels - width) / 2,
            (App.Window.HeightInPixels - height) / 2,
            width,
            height);
    }
}
using System.Numerics;
using Foster.Framework;
using Moggy.Assets;

namespace Moggy;

public sealed class HudSystem : GameSystem
{
    private FontAsset _font = null!;

    public override void Startup()
    {
        _font = Assets.Load<FontAsset>("Fonts/JoystixMonospace");
    }

    public override void Render(Time time)
    {
        Batcher.Text(_font.Sprite, "Test", new Vector2(0, 0), Color.Red);
    }
}
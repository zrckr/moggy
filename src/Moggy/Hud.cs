using System.Numerics;
using Foster.Framework;
using Moggy.Assets;

namespace Moggy;

public sealed class HudSystem : GameSystem
{
    private const string PlayerOneLabel = "1UP";

    private const string Score = "000000";

    private FontAsset _font = null!;

    public override void Startup()
    {
        _font = Assets.Load<FontAsset>("Fonts/JoystixMonospace");
    }

    public override void Render(Time time)
    {
        ref var game = ref Registry.Singleton<GameRuntime>();
        var isPaused = game.IsPaused;
        Batcher.InVirtualScreen(() =>
        {
            Batcher.TextMonospaced(_font.Sprite, PlayerOneLabel, Layout.PlayerOnePosition, Layout.PlayerOneColor);
            Batcher.TextMonospaced(_font.Sprite, Score, Layout.PlayerOneScorePosition, Color.White);

            if (isPaused)
            {
                Batcher.TextMonospaced(_font.Sprite, "PAUSED", Layout.PausedPosition, Color.Red);
            }
        });
    }

    private static class Layout
    {
        public static readonly Vector2 PlayerOnePosition = new(8, 0);
        public static readonly Vector2 PlayerOneScorePosition = new(8, 8);
        public static readonly Vector2 PausedPosition = new(96, 128);
        public static readonly Color PlayerOneColor = Color.FromHexStringRGB("#00BFFF");
    }
}

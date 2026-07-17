using System.Numerics;
using Foster.Framework;
using JetBrains.Annotations;
using Moggy.Assets;

namespace Moggy;

public sealed class HudSystem : GameSystem
{
    private const string Paused = "Paused";

    private const string PlayerOneLabel = "1UP";

    private const string PlayerTwoLabel = "2UP";

    private const string Score = "000000";

    private FontAsset _font = null!;

    public override void Startup()
    {
        _font = Assets.Load<FontAsset>("Fonts/JoystixMonospace");
    }

    public override void Render(Time time)
    {
        Batcher.InVirtualScreen(() =>
        {
            if (Game.State == GameState.Paused)
            {
                Batcher.TextMonospaced(_font.Sprite, Paused, Layout.PausedPosition, Color.Red);
            }

            Batcher.TextMonospaced(_font.Sprite, PlayerOneLabel, Layout.PlayerOnePosition, Layout.PlayerOneColor);
            Batcher.TextMonospaced(_font.Sprite, Score, Layout.PlayerOneScorePosition, Color.White);
            Batcher.TextMonospaced(_font.Sprite, PlayerTwoLabel, Layout.PlayerTwoPosition, Layout.PlayerTwoColor);
            Batcher.TextMonospaced(_font.Sprite, Score, Layout.PlayerTwoScorePosition, Color.White);
        });
    }

    private static class Layout
    {
        public static readonly Vector2 PausedPosition = new(96, 152);
        public static readonly Vector2 PlayerOnePosition = new(8, 0);
        public static readonly Vector2 PlayerTwoPosition = new(184, 0);
        public static readonly Vector2 PlayerOneScorePosition = new(8, 8);
        public static readonly Vector2 PlayerTwoScorePosition = new(184, 8);
        public static readonly Color PlayerOneColor = Color.FromHexStringRGB("#00BFFF");
        public static readonly Color PlayerTwoColor = Color.FromHexStringRGB("#800080");
    }
}

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

    private Layout _layout = null!;

    public override void Startup()
    {
        _font = Assets.Load<FontAsset>("Fonts/JoystixMonospace");
        _layout = Assets.LoadJson<Layout>("Hud/Layout");
    }

    public override void Render(Time time)
    {
        Batcher.InVirtualScreen(() =>
        {
            if (Game.State == GameState.Paused)
            {
                Batcher.TextMonospaced(_font.Sprite, Paused, _layout.PausedPosition, Color.Red);
            }

            Batcher.TextMonospaced(_font.Sprite, PlayerOneLabel, _layout.PlayerOnePosition, _layout.PlayerOneColor);
            Batcher.TextMonospaced(_font.Sprite, Score, _layout.PlayerOneScorePosition, Color.White);
            Batcher.TextMonospaced(_font.Sprite, PlayerTwoLabel, _layout.PlayerTwoPosition, _layout.PlayerTwoColor);
            Batcher.TextMonospaced(_font.Sprite, Score, _layout.PlayerTwoScorePosition, Color.White);
        });
    }

    [UsedImplicitly]
    private record Layout(
        Vector2 PausedPosition,
        Vector2 PlayerOnePosition,
        Vector2 PlayerTwoPosition,
        Vector2 PlayerOneScorePosition,
        Vector2 PlayerTwoScorePosition,
        Color PlayerOneColor,
        Color PlayerTwoColor
    );
}

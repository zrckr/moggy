using System.Globalization;
using System.Numerics;
using Foster.Framework;
using Moggy.Assets;

namespace Moggy;

public sealed class ScoreScreenGameSystem : GameSystem
{
    private const string VictoryLabel = "VICTORY";

    private const string DefeatLabel = "GAME OVER";

    private const string ScoreLabel = "SCORE";

    private const string RestartLabel = "PRESS ENTER TO RESTART";

    private FontAsset _font = null!;

    public override void Startup()
    {
        _font = Assets.Load<FontAsset>("Fonts/JoystixMonospace");
    }

    public override void Update(Time time)
    {
        if (Game.Input.Keyboard.Pressed(Keys.Enter))
        {
            // Entering the level initializes a fresh single-level run.
            Game.TransitionTo(GameState.Level);
        }
    }

    public override void Render(Time time)
    {
        ref var runtime = ref Registry.Singleton<LevelRuntime>();
        var outcome = runtime.State switch
        {
            LevelState.Victory => VictoryLabel,
            LevelState.Defeat => DefeatLabel,
            _ => throw new InvalidOperationException("The score screen requires a completed level.")
        };

        var score = runtime.Score.ToString(CultureInfo.InvariantCulture);
        Batcher.InVirtualScreen(() =>
        {
            DrawCentered(outcome, Layout.OutcomeY, Color.White);
            DrawCentered($"{ScoreLabel} {score}", Layout.ScoreY, Color.White);
            DrawCentered(RestartLabel, Layout.RestartY, Color.White);
        });
    }

    private void DrawCentered(string text, float y, Color color)
    {
        var width = text.Length * Mathz.TileSize;
        var position = new Vector2((Mathz.VirtualWidth - width) / 2f, y);
        Batcher.TextMonospaced(_font.Sprite, text, position, color);
    }

    private static class Layout
    {
        public const float OutcomeY = 96f;

        public const float ScoreY = 120f;

        public const float RestartY = 152f;
    }
}

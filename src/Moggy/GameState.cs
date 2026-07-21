using Foster.Framework;
using Serilog;

namespace Moggy;

public enum GameState
{
    Level,
    Score,
    Menu
}

public struct GameRuntime()
{
    public GameState State = GameState.Level;
    public GameState? NextState;
    public bool IsPaused = true; // TODO: Debug only
}

public sealed class GameRuntimeSystem : GameSystem
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<GameRuntimeSystem>();

    public override void Startup()
    {
        Registry.Create(new GameRuntime());
    }

    public override void Update(Time time)
    {
        ref var runtime = ref Registry.Singleton<GameRuntime>();
        switch (runtime.State)
        {
            case GameState.Level:
                UpdateLevelState();
                break;

            case GameState.Score:
                UpdateScoreState();
                break;

            case GameState.Menu:
                UpdateMenuState();
                break;

            default:
                Logger.Error("Unknown game state: {0}", runtime.State);
                break;
        }
    }

    private void UpdateLevelState()
    {
    }

    private void UpdateScoreState()
    {
        ref var runtime = ref Registry.Singleton<GameRuntime>();
        if (Game.Input.Keyboard.Pressed(Keys.Enter))
        {
            runtime.NextState = GameState.Level;
        }
    }

    private void UpdateMenuState()
    {
        ref var runtime = ref Registry.Singleton<GameRuntime>();
        if (Game.Input.Keyboard.Pressed(Keys.Enter))
        {
            runtime.NextState = GameState.Level;
        }
    }
}

using Foster.Framework;
using Serilog;

namespace Moggy;

public enum LevelState
{
    Ready,
    Active,
    Victory,
    Defeat
}

public struct LevelRuntime()
{
    public LevelState State = LevelState.Ready;
    public int Lives = 0;
    public int Score = 0;
}

public sealed class LevelRuntimeSystem : GameSystem, ILevelParticipant
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<LevelRuntimeSystem>();

    private LevelProperties _properties = null!;

    public override void Startup()
    {
        _properties = Assets.LoadJson<LevelProperties>("LevelProperties");
        Registry.Create(new LevelRuntime());
    }

    public void EnterLevel(LevelStartMode mode)
    {
        ref var runtime = ref Registry.Singleton<LevelRuntime>();
        runtime.State = LevelState.Ready;
        runtime.Lives = _properties.StartingLives;
        runtime.Score = 0;
    }

    public override void Update(Time time)
    {
        ref var game = ref Registry.Singleton<GameRuntime>();
        if (game.State != GameState.Level)
        {
            return;
        }

        ref var runtime = ref Registry.Singleton<LevelRuntime>();
        switch (runtime.State)
        {
            case LevelState.Ready:
                UpdateReadyState();
                break;

            case LevelState.Active:
                UpdateActiveState();
                break;

            case LevelState.Victory:
                UpdateVictoryState();
                break;

            case LevelState.Defeat:
                UpdateDefeatState();
                break;

            default:
                Logger.Error("Unknown level state: {0}", runtime.State);
                break;
        }
    }

    private void UpdateReadyState()
    {
        ref var runtime = ref Registry.Singleton<LevelRuntime>();
        if (Game.Input.Keyboard.Pressed(Keys.Enter))
        {
            runtime.State = LevelState.Active;
        }
    }

    private void UpdateActiveState()
    {
        ref var runtime = ref Registry.Singleton<LevelRuntime>();
        if (runtime.Score >= _properties.TargetScore)
        {
            runtime.State = LevelState.Victory;
            return;
        }

        if (runtime.Lives <= 0)
        {
            runtime.State = LevelState.Defeat;
        }
    }

    private void UpdateVictoryState()
    {
        ref var game = ref Registry.Singleton<GameRuntime>();
        game.NextState = GameState.Score;
    }

    private void UpdateDefeatState()
    {
    }
}

public enum LevelStartMode
{
    Initial,
    Restart
}

public interface ILevelParticipant
{
    void EnterLevel(LevelStartMode mode)
    {
    }

    void ExitLevel()
    {
    }
}

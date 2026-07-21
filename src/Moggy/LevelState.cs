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
    public int ObjectiveProgress = 0;   // Score Attack
}

public sealed class LevelRuntimeSystem : GameSystem, ILevelParticipant
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<LevelRuntimeSystem>();

    public override void Startup()
    {
        Registry.Create(new LevelRuntime());
    }

    public void EnterLevel(LevelStartMode mode)
    {
        ref var runtime = ref Registry.Singleton<LevelRuntime>();
        runtime.State = LevelState.Ready;
        runtime.Lives = 0;
        runtime.Score = 0;
        runtime.ObjectiveProgress = 0;
    }

    public void ExitLevel()
    {
    }

    public override void Update(Time time)
    {
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
    }

    private void UpdateActiveState()
    {
    }

    private void UpdateVictoryState()
    {
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
    void EnterLevel(LevelStartMode mode);

    void ExitLevel();
}

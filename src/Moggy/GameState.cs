using Foster.Framework;

namespace Moggy;

public enum GameState
{
    Level,
    Score,
    Menu
}

public struct GameRuntime()
{
    public bool IsPaused = true; // TODO: Debug only
}

public sealed class GameRuntimeSystem : GameSystem
{
    public override void Startup()
    {
        Registry.Create(new GameRuntime());
    }

    public override void Update(Time time)
    {
        switch (Game.State)
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
        }
    }

    private void UpdateLevelState()
    {
    }

    private void UpdateScoreState()
    {
        if (Game.Input.Keyboard.Pressed(Keys.Enter))
        {
            Game.TransitionTo(GameState.Level);
        }
    }

    private void UpdateMenuState()
    {
        if (Game.Input.Keyboard.Pressed(Keys.Enter))
        {
            Game.TransitionTo(GameState.Level);
        }
    }
}

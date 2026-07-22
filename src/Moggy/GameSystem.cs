using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public abstract class GameSystem
{
    public Game Game { protected get; init; } = null!;

    public Registry Registry { protected get; init; } = null!;

    public AssetLoader Assets { protected get; init; } = null!;

    public Batcher Batcher { protected get; init; } = null!;

    public virtual void Startup()
    {
    }

    public virtual void Update(Time time)
    {
    }

    public virtual void Render(Time time)
    {
    }

    public virtual void Shutdown()
    {
    }
}

public sealed class GameSystemGroup : GameSystem
{
    private readonly List<GameSystem> _systems;

    public GameSystemGroup(params IEnumerable<GameSystem> systems)
    {
        _systems = new List<GameSystem>(systems);
    }

    public override void Startup()
    {
        foreach (var system in _systems)
        {
            system.Startup();
        }
    }

    public override void Update(Time time)
    {
        foreach (var system in _systems)
        {
            system.Update(time);
        }
    }

    public override void Render(Time time)
    {
        foreach (var system in _systems)
        {
            system.Render(time);
        }
    }

    public override void Shutdown()
    {
        for (var index = _systems.Count - 1; index >= 0; index--)
        {
            _systems[index].Shutdown();
        }
    }

    public void Enter()
    {
        foreach (var system in _systems)
        {
            if (system is GameSystemGroup group)
            {
                group.Enter();
                continue;
            }

            if (system is IGameSystemGroupState state)
            {
                state.Enter();
            }
        }
    }

    public void Exit()
    {
        for (var index = _systems.Count - 1; index >= 0; index--)
        {
            var system = _systems[index];
            if (system is GameSystemGroup group)
            {
                group.Exit();
                continue;
            }

            if (system is IGameSystemGroupState state)
            {
                state.Exit();
            }
        }
    }
}

public sealed class GameSystemGroup<TState> : GameSystem where TState : struct, Enum
{
    public TState State { get; private set; }

    private readonly Dictionary<TState, GameSystemGroup> _groups;

    private readonly List<GameSystemGroup> _orderedGroups;

    private TState? _nextState;

    public GameSystemGroup(TState initialState, IReadOnlyDictionary<TState, GameSystemGroup> groups)
    {
        State = initialState;
        _groups = new Dictionary<TState, GameSystemGroup>(groups);
        _orderedGroups = new List<GameSystemGroup>(groups.Values);

        if (!_groups.ContainsKey(initialState))
        {
            throw new ArgumentException("The initial state must have a system group.", nameof(initialState));
        }
    }

    public void TransitionTo(TState state)
    {
        if (!_groups.ContainsKey(state))
        {
            throw new ArgumentException("The state must have a system group.", nameof(state));
        }

        _nextState = state;
    }

    public void Restart()
    {
        _nextState = null;
        _groups[State].Exit();
        _groups[State].Enter();
    }

    public override void Startup()
    {
        foreach (var group in _orderedGroups)
        {
            group.Startup();
        }

        _groups[State].Enter();
    }

    public override void Update(Time time)
    {
        _groups[State].Update(time);

        // Defer activation changes until the current state finishes updating.
        if (_nextState is not { } nextState)
        {
            return;
        }

        _nextState = null;
        if (EqualityComparer<TState>.Default.Equals(State, nextState))
        {
            return;
        }

        _groups[State].Exit();
        State = nextState;
        _groups[State].Enter();
    }

    public override void Render(Time time)
    {
        _groups[State].Render(time);
    }

    public override void Shutdown()
    {
        _groups[State].Exit();

        for (var index = _orderedGroups.Count - 1; index >= 0; index--)
        {
            _orderedGroups[index].Shutdown();
        }
    }
}

public interface IGameSystemGroupState
{
    void Enter()
    {
    }

    void Exit()
    {
    }
}

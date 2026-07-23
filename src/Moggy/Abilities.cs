using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public interface IAbility : ITag;

public sealed record AbilityProperties
{
    public float MovementSpeed { get; init; }

    public TimeSpan Duration { get; init; }

    public float Scale { get; init; } = 1f;

    public string IdleSprite { get; init; } = string.Empty;

    public string MoveSprite { get; init; } = string.Empty;
}

public struct Abilities()
{
    public int Counter = 0;
    public TimeSpan Remaining = TimeSpan.Zero;
}

public readonly record struct AbilityEntered<T>(Entity Player) where T : struct, IAbility;

public readonly record struct AbilityExited<T>(Entity Player) where T : struct, IAbility;

public sealed class AbilitiesSystem : GameSystem, IGameSystemGroupState
{
    private LevelProperties _properties = null!;

    private AbilityProperties _bigBoyProperties = null!;

    private AbilityProperties _microManProperties = null!;

    private Random _random = null!;

    private Entity _playerEntity = Entity.Invalid;

    public override void Startup()
    {
        _properties = Assets.LoadJson<LevelProperties>("LevelProperties");
        _bigBoyProperties = Assets.LoadJson<AbilityProperties>("Player/BigBoy");
        _microManProperties = Assets.LoadJson<AbilityProperties>("Player/MicroMan");
        _random = new Random();
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<LevelRuntime>();
        if (level.State != LevelState.Active)
        {
            return;
        }

        ref var runtime = ref Registry.Get<Abilities>(_playerEntity);
        if (!Registry.Has<Normal>(_playerEntity))
        {
            runtime.Remaining -= TimeSpan.FromSeconds(time.Delta);
            if (runtime.Remaining <= TimeSpan.Zero)
            {
                runtime.Remaining = TimeSpan.Zero;
                SetAbility<Normal>();
                return;
            }
        }

        // TODO: make this dynamic later
        if (runtime.Counter >= _properties.AbilityTrigger)
        {
            if (_random.NextDouble() < _properties.AbilityOdds)
            {
                SetAbility<BigBoy>();
                runtime.Remaining = _bigBoyProperties.Duration;
            }
            else
            {
                SetAbility<MicroMan>();
                runtime.Remaining = _microManProperties.Duration;
            }

            runtime.Counter = 0;
        }
    }

    public void Enter()
    {
        _playerEntity = Registry.Query<Player, Abilities>().Single();
        ref var runtime = ref Registry.Get<Abilities>(_playerEntity);
        runtime = new Abilities();

        Registry.RemoveAll<IAbility>(_playerEntity);
        Registry.SetTag<Normal>(_playerEntity);
        Registry.Set(_playerEntity, new AbilityEntered<Normal>(_playerEntity));
    }

    public void Exit()
    {
        Registry.RemoveAll<IAbility>(_playerEntity);
        _playerEntity = Entity.Invalid;
    }

    private void SetAbility<T>() where T : struct, IAbility
    {
        if (Registry.Has<T>(_playerEntity))
        {
            Registry.Set(_playerEntity, new AbilityExited<T>(_playerEntity));
        }

        Registry.RemoveAll<IAbility>(_playerEntity);
        Registry.SetTag<T>(_playerEntity);
        Registry.Set(_playerEntity, new AbilityEntered<T>(_playerEntity));
    }
}

using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct Normal : IAbility;

public sealed class NormalSystem : GameSystem
{
    private Query _player = null!;

    private AbilityProperties _properties = null!;

    public override void Startup()
    {
        _player = Registry.Query()
            .Include<Normal>()
            .Include<Player>()
            .Include<Sprite>()
            .Build();

        _properties = Assets.LoadJson<AbilityProperties>("Player/Normal");
    }

    public override void Update(Time time)
    {
        ref var game = ref Registry.Singleton<GameRuntime>();
        if (game.State != GameState.Level)
        {
            return;
        }

        if (!_player.Any())
        {
            return;
        }

        var playerEntity = _player.Single();
        ref var sprite = ref Registry.Get<Sprite>(playerEntity);
        sprite.Transform.Scale = Vector2.One * _properties.Scale;

        if (Registry.Has<LevelMover>(playerEntity))
        {
            ref var mover = ref Registry.Get<LevelMover>(playerEntity);
            mover.Speed = _properties.MovementSpeed;
        }
    }
}

using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct MicroMan : IAbility;

public sealed class MicroManSystem : GameSystem
{
    private Query _player = null!;

    private AbilityProperties _properties = null!;

    public override void Startup()
    {
        _player = Registry.Query()
            .Include<MicroMan>()
            .Include<Player>()
            .Include<Sprite>()
            .Build();

        _properties = Assets.LoadJson<AbilityProperties>("Player/MicroMan");
    }

    public override void Update(Time time)
    {
        ref var game = ref Registry.Singleton<GameRuntime>();
        if (game.State != GameState.Level)
        {
            return;
        }

        ref var level = ref Registry.Singleton<LevelRuntime>();
        if (level.State != LevelState.Active)
        {
            return;
        }

        if (!_player.Any())
        {
            return;
        }

        var playerEntity = _player.Single();
        ref var player = ref Registry.Get<Player>(playerEntity);
        ref var sprite = ref Registry.Get<Sprite>(playerEntity);

        sprite.Transform.Scale = Vector2.One * _properties.Scale;
        sprite.Animation.SetName(player.Direction.GetAnimationName());
        sprite.FlipH = player.Direction.IsAnimationFlipped();

        if (Registry.Has<LevelMover>(playerEntity))
        {
            ref var mover = ref Registry.Get<LevelMover>(playerEntity);
            mover.Speed = _properties.MovementSpeed;
        }
    }
}

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
        _player = Registry.Query<Normal, Player, Sprite>();
        _properties = Assets.LoadJson<AbilityProperties>("Player/Normal");
    }

    public override void Update(Time time)
    {
        if (!_player.Any())
        {
            return;
        }

        var playerEntity = _player.Single();
        ref var player = ref Registry.Get<Player>(playerEntity);
        ref var sprite = ref Registry.Get<Sprite>(playerEntity);

        player.MovementSpeed = _properties.MovementSpeed;
        sprite.Transform.Scale = Vector2.One * _properties.Scale;
    }
}

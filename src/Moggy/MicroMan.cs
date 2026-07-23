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
            .Include<Piece>()
            .Include<Sprite>()
            .Build();

        _properties = Assets.LoadJson<AbilityProperties>("Player/MicroMan");
    }

    public override void Update(Time time)
    {
        if (!_player.Any())
        {
            return;
        }

        var playerEntity = _player.Single();
        ref var player = ref Registry.Get<Player>(playerEntity);
        ref var piece = ref Registry.Get<Piece>(playerEntity);
        ref var sprite = ref Registry.Get<Sprite>(playerEntity);

        player.MovementSpeed = _properties.MovementSpeed;
        sprite.Transform.Scale = Vector2.One * _properties.Scale;
        sprite.Animation.SetName(piece.FacingDirection.GetAnimationName());
        sprite.FlipH = piece.FacingDirection.IsAnimationFlipped();
    }
}

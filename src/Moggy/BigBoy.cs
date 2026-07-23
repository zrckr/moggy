using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public struct BigBoy : IAbility;

public sealed class BigBoySystem : GameSystem
{
    private const string HorizontalAnimation = "horizontal";

    private const string VerticalAnimation = "vertical";

    private Query _player = null!;

    private AbilityProperties _properties = null!;

    private SpriteAsset _sprite = null!;

    public override void Startup()
    {
        _player = Registry.Query<BigBoy, Player, Piece, Sprite>();
        _properties = Assets.LoadJson<AbilityProperties>("Player/BigBoy");
        _sprite = Assets.Load<SpriteAsset>(_properties.MoveSprite);
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
        sprite.Asset = _sprite;
        sprite.Transform.Scale = Vector2.One * _properties.Scale;
        sprite.Animation.SetName(piece.FacingDirection.GetAnimationName2D());
        sprite.FlipH = piece.FacingDirection.IsAnimationFlipped();
    }

    public override void Shutdown()
    {
        _sprite.Dispose();
    }
}

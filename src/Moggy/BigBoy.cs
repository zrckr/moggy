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
        _player = Registry.Query()
            .Include<BigBoy>()
            .Include<Player>()
            .Include<Sprite>()
            .Build();

        _properties = Assets.LoadJson<AbilityProperties>("Player/BigBoy");
        _sprite = Assets.Load<SpriteAsset>(_properties.MoveSprite);
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

        var animationName = player.Direction is FaceDirection.Up or FaceDirection.Down
            ? VerticalAnimation
            : HorizontalAnimation;

        sprite.Asset = _sprite;
        sprite.Transform.Scale = Vector2.One * _properties.Scale;
        sprite.Animation.SetName(animationName);
        sprite.FlipH = player.Direction.IsAnimationFlipped();

        if (Registry.Has<LevelMover>(playerEntity))
        {
            ref var mover = ref Registry.Get<LevelMover>(playerEntity);
            mover.Speed = _properties.MovementSpeed;
        }
    }

    public override void Shutdown()
    {
        _sprite.Dispose();
    }
}

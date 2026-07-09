using System.Numerics;
using Moggy.Assets;

namespace Moggy;

public sealed class Player : GameSystem
{
    private AssetId _idleSprite;

    public override void Startup()
    {
        _idleSprite = Assets.Load<Sprite>("Player/Idle", out var sprite);
        var earsAnimation = sprite!.GetAnimationIndex("ears");

        var player = Registry.Create();
        Registry.Set(player, new SpriteRenderer
        {
            SpriteId = _idleSprite,
            Position = Vector2.Zero,
            PixelSize = 2
        });

        Registry.Set(player, new AnimatedSprite
        {
            AnimationIndex = earsAnimation,
            Loop = true
        });
    }

    public override void Shutdown()
    {
        Assets.Unload(_idleSprite);
    }
}
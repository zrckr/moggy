using System.Numerics;
using Moggy.Assets;

namespace Moggy;

public sealed class PlayerSystem : GameSystem
{
    public override void Startup()
    {
        var spriteId = Assets.Load<Sprite>("Player/Idle", out var sprite);
        var defaultAnimation = sprite!.GetAnimationIndex("ears");
        if (defaultAnimation < 0)
        {
            throw new InvalidOperationException($"Sprite '{sprite.Name}' does not contain the 'default' animation tag.");
        }

        var player = Registry.Create();
        Registry.Set(player, new SpriteRenderer
        {
            SpriteId = spriteId,
            Position = new Vector2(App.Window.Width * 0.5f, App.Window.Height * 0.5f),
            Origin = sprite.Frames[0].Subtexture.Size * 0.5f,
            PixelSize = 8
        });

        Registry.Set(player, new AnimatedSprite
        {
            AnimationIndex = defaultAnimation,
            Loop = true
        });
    }
}
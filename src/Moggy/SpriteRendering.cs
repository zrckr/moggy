using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Serilog;

namespace Moggy;

public struct SpriteRenderer()
{
    public AssetId SpriteId;
    public int FrameIndex;
    public Vector2 Position;
    public Vector2? ForceOrigin;
    public int PixelSize = 1;
    public bool FlipHorizontal;
    public bool FlipVertical;
}

public struct AnimatedSprite
{
    public int AnimationIndex;
    public TimeSpan Time;
    public bool Loop;
}

public sealed class SpriteRendering : GameSystem
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<SpriteRendering>();

    private Query _animatedSprites = null!;

    private Query _sprites = null!;

    public override void Startup()
    {
        _animatedSprites = Registry.Query()
            .Include<AnimatedSprite>()
            .Build();

        _sprites = Registry.Query()
            .Include<SpriteRenderer>()
            .Build();
    }

    public override void Update(Time time)
    {
        foreach (var entity in _animatedSprites)
        {
            if (!Registry.Has<SpriteRenderer>(entity))
            {
                Logger.Warning("The entity ID={0} with animated sprite doesn't have sprite renderer.", entity.Id);
                break;
            }

            ref var animation = ref Registry.Get<AnimatedSprite>(entity);
            animation.Time += TimeSpan.FromSeconds(time.Delta);
        }
    }

    public override void Render(Time time)
    {
        Batcher.PushSampler(new TextureSampler(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));
        foreach (var entity in _sprites)
        {
            ref var renderer = ref Registry.Get<SpriteRenderer>(entity);
            if (!Assets.TryGet<Sprite>(renderer.SpriteId, out var sprite))
            {
                continue;
            }

            Sprite.Frame frame;
            if (Registry.Has<AnimatedSprite>(entity))
            {
                ref var animation = ref Registry.Get<AnimatedSprite>(entity);
                var animationIndex = Math.Clamp(animation.AnimationIndex, 0, sprite!.Animations.Count - 1);
                frame = sprite.GetFrameAt(sprite.Animations[animationIndex], animation.Time, animation.Loop);

            }
            else
            {
                var frameIndex = Math.Clamp(renderer.FrameIndex, 0, sprite!.Frames.Count - 1);
                frame = sprite.Frames[frameIndex];
            }

            var origin = renderer.ForceOrigin ?? sprite.Frames[0].Subtexture.Size * 0.5f;
            var scale = Vector2.One * Math.Max(1, renderer.PixelSize);
            if (renderer.FlipHorizontal) scale.X *= -1;
            if (renderer.FlipVertical) scale.Y *= -1;

            var position = SnapPosition(renderer.Position, origin, scale);
            Batcher.Image(frame.Subtexture, position, origin, scale, 0, Color.White);
        }
        Batcher.PopSampler();
    }

    private static Vector2 SnapPosition(Vector2 position, Vector2 origin, Vector2 scale)
    {
        var topLeft = position - origin * scale;
        return new Vector2(MathF.Round(topLeft.X), MathF.Round(topLeft.Y)) + origin * scale;
    }
}
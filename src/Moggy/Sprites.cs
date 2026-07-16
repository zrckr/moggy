using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Serilog;

namespace Moggy;

public struct SpriteTransform
{
    public Vector2 Position;
    public Vector2 Scale;
    public float Rotation;
}

public struct StaticSprite
{
    public AssetId Sprite;
    public bool FlipHorizontal;
    public bool FlipVertical;
    public Vector2 Offset;
}

public struct AnimatedSprite
{
    public string Animation
    {
        get;
        set
        {
            if (!string.Equals(field, value, StringComparison.OrdinalIgnoreCase))
            {
                field = value;
                Frame = 0;
            }
        }
    }

    public AssetId Sprite
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                Frame = 0;
            }
        }
    }

    public int Frame;
    public bool FlipHorizontal;
    public bool FlipVertical;
    public Vector2 Offset;
}

public sealed class SpritesSystem : GameSystem
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<SpritesSystem>();

    private readonly Dictionary<Entity, TimeSpan> _animationTimes = new();

    private Query _animatedSprites = null!;

    private Query _staticSprites = null!;

    public override void Startup()
    {
        _animatedSprites = Registry.Query()
            .Include<SpriteTransform>()
            .Include<AnimatedSprite>()
            .Build();

        _staticSprites = Registry.Query()
            .Include<SpriteTransform>()
            .Include<StaticSprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        foreach (var entity in _animatedSprites)
        {
            ref var renderer = ref Registry.Get<AnimatedSprite>(entity);
            UpdateAnimation(entity, ref renderer, TimeSpan.FromSeconds(time.Delta));
        }
    }

    public override void Render(Time time)
    {
        Batcher.PushSampler(new TextureSampler(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));

        foreach (var entity in _staticSprites)
        {
            ref var transform = ref Registry.Get<SpriteTransform>(entity);
            ref var renderer = ref Registry.Get<StaticSprite>(entity);
            RenderStaticSprite(in transform, in renderer);
        }

        foreach (var entity in _animatedSprites)
        {
            ref var transform = ref Registry.Get<SpriteTransform>(entity);
            ref var renderer = ref Registry.Get<AnimatedSprite>(entity);
            RenderAnimatedSprite(entity, in transform, in renderer);
        }

        Batcher.PopSampler();
    }

    private void UpdateAnimation(Entity entity, ref AnimatedSprite renderer, TimeSpan elapsed)
    {
        if (!TryGetAnimation(entity, in renderer, out var sprite, out var animation) || animation.Count <= 1)
        {
            _animationTimes.Remove(entity);
            renderer.Frame = 0;
            return;
        }

        var frame = Math.Clamp(renderer.Frame, 0, animation.Count - 1);
        var frameTime = _animationTimes.GetValueOrDefault(entity) + elapsed;

        while (frameTime >= sprite.Frames[animation.Start + frame].Duration)
        {
            frameTime -= sprite.Frames[animation.Start + frame].Duration;
            frame = (frame + 1) % animation.Count;
        }

        _animationTimes[entity] = frameTime;
        renderer.Frame = frame;
    }

    private void RenderStaticSprite(in SpriteTransform spriteTransform, in StaticSprite renderer)
    {
        if (Assets.TryGet<SpriteAsset>(renderer.Sprite, out var sprite) && sprite is not null && sprite.Frames.Count != 0)
        {
            RenderFrame(sprite.Frames[0], spriteTransform, renderer.Offset, renderer.FlipHorizontal, renderer.FlipVertical);
        }
    }

    private void RenderAnimatedSprite(Entity entity, in SpriteTransform spriteTransform, in AnimatedSprite renderer)
    {
        if (!TryGetAnimation(entity, in renderer, out var sprite, out var animation))
        {
            return;
        }

        var frameOffset = Math.Clamp(renderer.Frame, 0, animation.Count - 1);
        var frame = sprite.Frames[animation.Start + frameOffset];
        RenderFrame(frame, spriteTransform, renderer.Offset, renderer.FlipHorizontal, renderer.FlipVertical);
    }

    private bool TryGetAnimation(
        Entity entity,
        in AnimatedSprite renderer,
        out SpriteAsset sprite,
        out SpriteAsset.Animation animation)
    {
        if (!Assets.TryGet<SpriteAsset>(renderer.Sprite, out var resolvedSprite) || resolvedSprite is null)
        {
            sprite = null!;
            animation = default;
            return false;
        }

        var animationName = string.IsNullOrWhiteSpace(renderer.Animation) ? "default" : renderer.Animation;
        var resolvedAnimation = resolvedSprite.GetAnimation(animationName);
        if (resolvedAnimation is null)
        {
            Logger.Warning("Entity ID={EntityId} references missing animation '{Animation}'",
                entity.Id, animationName);
            sprite = null!;
            animation = default;
            return false;
        }

        sprite = resolvedSprite;
        animation = resolvedAnimation.Value;
        return true;
    }

    private void RenderFrame(SpriteAsset.Frame frame, in SpriteTransform spriteTransform, Vector2 offset, bool flipH, bool flipV)
    {
        var origin = frame.Subtexture.Size * 0.5f;
        var scale = spriteTransform.Scale;
        if (flipH) scale.X *= -1;
        if (flipV) scale.Y *= -1;

        var position = SnapPosition(spriteTransform.Position + offset, origin, scale);
        Batcher.Image(frame.Subtexture, position, origin, scale, spriteTransform.Rotation, Color.White);
    }

    private static Vector2 SnapPosition(Vector2 position, Vector2 origin, Vector2 scale)
    {
        var topLeft = position - origin * scale;
        return new Vector2(MathF.Round(topLeft.X), MathF.Round(topLeft.Y)) + origin * scale;
    }
}
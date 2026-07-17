using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Serilog;

namespace Moggy;

public struct Sprite()
{
    public AssetId Asset
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                if (Animation is { } animation)
                {
                    animation.Reset();
                    Animation = animation;
                }
            }
        }
    } = AssetId.Invalid;

    public Transform Transform = Transform.Identity;
    public Vector2 Offset = Vector2.Zero;
    public bool FlipH = false;
    public bool FlipV = false;
    public SpriteAnimation? Animation = null;
}

public struct SpriteAnimation(string name)
{
    public string Name
    {
        get;
        set
        {
            if (!string.Equals(field, value, StringComparison.OrdinalIgnoreCase))
            {
                field = value;
                Reset();
            }
        }
    } = name;

    public int Frame;

    public TimeSpan AnimationTime;

    public void Reset()
    {
        Frame = 0;
        AnimationTime = TimeSpan.Zero;
    }
}

public static class SpriteAnimationExtensions
{
    public static void SetName(this ref SpriteAnimation? animation, string name)
    {
        if (animation is { } value)
        {
            value.Name = name;
            animation = value;
        }
    }
}

public sealed class SpriteSystem : GameSystem
{
    private static readonly ILogger Logger = Serilog.Log.ForContext<SpriteSystem>();

    private Query _sprites = null!;

    public override void Startup()
    {
        _sprites = Registry.Query()
            .Include<Sprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        foreach (var entity in _sprites)
        {
            ref var sprite = ref Registry.Get<Sprite>(entity);
            if (sprite.Animation is { } animation)
            {
                UpdateAnimation(entity, sprite.Asset, ref animation, TimeSpan.FromSeconds(time.Delta));
                sprite.Animation = animation;
            }
        }
    }

    public override void Render(Time time)
    {
        Batcher.PushSampler(new TextureSampler(TextureFilter.Nearest, TextureWrap.Clamp, TextureWrap.Clamp));

        foreach (var entity in _sprites)
        {
            ref var sprite = ref Registry.Get<Sprite>(entity);
            if (sprite.Animation is { } animation)
            {
                RenderAnimatedSprite(entity, in sprite, in animation);
            }
            else
            {
                RenderStaticSprite(in sprite);
            }
        }

        Batcher.PopSampler();
    }

    private void UpdateAnimation(Entity entity, AssetId spriteAsset, ref SpriteAnimation renderer, TimeSpan elapsed)
    {
        if (!TryGetAnimation(entity, spriteAsset, in renderer, out var sprite, out var animation) ||
            animation.Count <= 1)
        {
            renderer.AnimationTime = TimeSpan.Zero;
            renderer.Frame = 0;
            return;
        }

        var frame = Math.Clamp(renderer.Frame, 0, animation.Count - 1);
        var frameTime = renderer.AnimationTime + elapsed;

        while (frameTime >= sprite.Frames[animation.Start + frame].Duration)
        {
            frameTime -= sprite.Frames[animation.Start + frame].Duration;
            frame = (frame + 1) % animation.Count;
        }

        renderer.AnimationTime = frameTime;
        renderer.Frame = frame;
    }

    private void RenderStaticSprite(in Sprite renderer)
    {
        if (Assets.TryGet<SpriteAsset>(renderer.Asset, out var sprite) &&
            sprite is not null &&
            sprite.Frames.Count != 0)
        {
            RenderFrame(sprite.Frames[0], renderer.Transform, renderer.Offset, renderer.FlipH, renderer.FlipV);
        }
    }

    private void RenderAnimatedSprite(Entity entity, in Sprite renderer, in SpriteAnimation animationState)
    {
        if (TryGetAnimation(entity, renderer.Asset, in animationState, out var sprite, out var animation))
        {
            var frameOffset = Math.Clamp(animationState.Frame, 0, animation.Count - 1);
            var frame = sprite.Frames[animation.Start + frameOffset];
            RenderFrame(frame, renderer.Transform, renderer.Offset, renderer.FlipH, renderer.FlipV);
        }
    }

    private bool TryGetAnimation(
        Entity entity,
        AssetId spriteAsset,
        in SpriteAnimation renderer,
        out SpriteAsset sprite,
        out SpriteAsset.Animation animation)
    {
        if (!Assets.TryGet<SpriteAsset>(spriteAsset, out var resolvedSprite) || resolvedSprite is null)
        {
            sprite = null!;
            animation = default;
            return false;
        }

        var animationName = string.IsNullOrWhiteSpace(renderer.Name) ? "default" : renderer.Name;
        var resolvedAnimation = resolvedSprite.GetAnimation(animationName);
        if (resolvedAnimation is null)
        {
            Logger.Warning("Entity ID={EntityId} references missing animation '{Animation}'", entity.Id, animationName);
            sprite = null!;
            animation = default;
            return false;
        }

        sprite = resolvedSprite;
        animation = resolvedAnimation.Value;
        return true;
    }

    private void RenderFrame(SpriteAsset.Frame frame, in Transform transform, Vector2 offset, bool flipH, bool flipV)
    {
        var origin = frame.Subtexture.Size * 0.5f;
        var scale = transform.Scale;
        if (flipH)
        {
            scale.X *= -1;
        }

        if (flipV)
        {
            scale.Y *= -1;
        }

        // Snap the position
        var topLeft = transform.Position + offset - (origin * scale);
        var position = new Vector2(MathF.Round(topLeft.X), MathF.Round(topLeft.Y)) + (origin * scale);

        Batcher.Image(frame.Subtexture, position, origin, scale, transform.Rotation, Color.White);
    }
}

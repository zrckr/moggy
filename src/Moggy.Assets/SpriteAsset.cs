using System.Text.Json.Serialization;
using Foster.Framework;

namespace Moggy.Assets;

public sealed class SpriteAsset : AssetResource
{
    [JsonIgnore] public IReadOnlyList<Frame> Frames => _frames;

    [JsonIgnore] public IReadOnlyList<Animation> Animations => _animations;

    [JsonIgnore] public Texture Texture { get; private set; } = null!;

    private Frame[] _frames = null!;

    private Animation[] _animations = null!;

    public override void Load(AssetLoadContext context, Stream stream)
    {
        var aseprite = new Aseprite(stream);
        if (aseprite.Frames.Length == 0)
        {
            throw new InvalidOperationException($"Sprite '{Name}' does not contain frames.");
        }

        var renderedFrames = aseprite.RenderAllFrames();
        var packer = new Packer();
        for (var i = 0; i < renderedFrames.Length; i++)
        {
            packer.Add(i, $"{Name}/{i}",
                renderedFrames[i].Width, renderedFrames[i].Height, renderedFrames[i].Data);
        }

        var output = packer.Pack();
        foreach (var image in renderedFrames)
        {
            image.Dispose();
        }

        if (output.Pages.Count != 1)
        {
            foreach (var page in output.Pages)
            {
                page.Dispose();
            }

            throw new InvalidOperationException($"Sprite '{Name}' was too large for a single texture.");
        }

        Texture = new Texture(context.App.GraphicsDevice, output.Pages[0], Name);
        output.Pages[0].Dispose();

        _frames = new Frame[aseprite.Frames.Length];
        foreach (var entry in output.Entries)
        {
            _frames[entry.Index] = new Frame(
                new Subtexture(Texture, entry.Source, entry.Frame),
                TimeSpan.FromMilliseconds(aseprite.Frames[entry.Index].Duration));
        }

        var animations = new List<Animation>();
        foreach (var tag in aseprite.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag.Name))
            {
                continue;
            }

            AddAnimation(tag.Name, tag.From, tag.To - tag.From + 1);
        }

        if (animations.Count == 0)
        {
            AddAnimation("default", 0, _frames.Length);
        }

        _animations = animations.ToArray();
        return;

        void AddAnimation(string name, int start, int count)
        {
            var duration = TimeSpan.Zero;
            for (var i = start; i < start + count; i++)
            {
                duration += _frames[i].Duration;
            }

            animations.Add(new Animation(name, start, count, duration));
        }
    }

    public override void Dispose()
    {
        if (IsDisposing)
        {
            return;
        }

        base.Dispose();
        Texture.Dispose();
    }

    public Frame GetFrameAt(in Animation animation, TimeSpan time, bool loop)
    {
        if (animation.Count <= 0)
        {
            return _frames[0];
        }

        if (time >= animation.Duration && !loop)
        {
            return _frames[animation.Start + animation.Count - 1];
        }

        if (animation.Duration > TimeSpan.Zero)
        {
            time = TimeSpan.FromTicks(time.Ticks % animation.Duration.Ticks);
        }

        for (var i = animation.Start; i < animation.Start + animation.Count; i++)
        {
            time -= _frames[i].Duration;
            if (time <= TimeSpan.Zero)
            {
                return _frames[i];
            }
        }

        return _frames[animation.Start];
    }

    public Animation? GetAnimation(string name)
    {
        foreach (var animation in _animations)
        {
            if (string.Equals(animation.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return animation;
            }
        }

        return null;
    }

    public int GetAnimationIndex(string name)
    {
        for (var i = 0; i < _animations.Length; i++)
        {
            if (string.Equals(_animations[i].Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    public readonly record struct Frame(Subtexture Subtexture, TimeSpan Duration);

    public readonly record struct Animation(string Name, int Start, int Count, TimeSpan Duration);
}

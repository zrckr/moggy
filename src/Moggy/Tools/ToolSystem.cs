using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy.Tools;

public abstract class ToolSystem
{
    public App App { protected get; init; } = null!;

    public Registry Registry { protected get; init; } = null!;

    public AssetLoader Assets { protected get; init; } = null!;

    public bool IsOpen = false;

    public virtual string Title => GetType().Name;

    public virtual void Startup()
    {
    }

    public virtual void Draw(Time time)
    {
    }
}
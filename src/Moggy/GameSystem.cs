using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public abstract class GameSystem
{
    public App App { protected get; init; } = null!;

    public Registry Registry { protected get; init; } = null!;

    public AssetLoader Assets { protected get; init; } = null!;

    public Batcher Batcher { protected get; init; } = null!;

    public virtual void Startup()
    {
    }

    public virtual void Update(Time time)
    {
    }

    public virtual void Render(Time time)
    {
    }

    public virtual void Shutdown()
    {
    }
}
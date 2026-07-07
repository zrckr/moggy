using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public abstract class GameSystem
{
    public required Registry Registry { protected get; init; }

    public required GraphicsDevice GraphicsDevice { protected get; init; }

    public required Window Window { protected get; init; }

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
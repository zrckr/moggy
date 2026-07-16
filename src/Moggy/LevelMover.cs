using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct LevelPosition
{
    public Cell Cell;
}

public struct LevelMover
{
    public Cell From;
    public Cell To;
    public float Progress;
    public float Speed;
}

public sealed class LevelMoverSystem : GameSystem
{
    private Query _movers = null!;

    private readonly List<Entity> _completed = new();

    public override void Startup()
    {
        _movers = Registry.Query()
            .Include<LevelPosition>()
            .Include<LevelMover>()
            .Include<Transform>()
            .Build();
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();

        _completed.Clear();
        foreach (var entity in _movers)
        {
            ref var position = ref Registry.Get<LevelPosition>(entity);
            ref var mover = ref Registry.Get<LevelMover>(entity);
            ref var transform = ref Registry.Get<Transform>(entity);

            var from = level.CellToCenter(mover.From);
            var to = level.CellToCenter(mover.To);

            mover.Progress += mover.Speed * time.Delta;
            if (mover.Progress >= 1f)
            {
                position.Cell = mover.To;
                transform.Position = to;
                _completed.Add(entity);
                continue;
            }

            transform.Position = Vector2.Lerp(from, to, mover.Progress);
        }

        foreach (var entity in _completed)
        {
            Registry.Remove<LevelMover>(entity);
        }
    }
}
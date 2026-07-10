using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct GridPosition
{
    public Point2 Cell;
}

public struct GridMover
{
    public Point2 From;
    public Point2 To;
    public float Progress;
    public float Speed;
}

public sealed class GridMoverSystem : GameSystem
{
    private Query _grid = null!;

    private Query _movers = null!;

    private readonly List<Entity> _completed = new();

    public override void Startup()
    {
        _grid = Registry.Query()
            .Include<Grid>()
            .Build();

        _movers = Registry.Query()
            .Include<GridPosition>()
            .Include<GridMover>()
            .Include<Transform>()
            .Build();
    }

    public override void Update(Time time)
    {
        var gridEntity = _grid.Single();
        ref var grid = ref Registry.Get<Grid>(gridEntity);

        _completed.Clear();
        foreach (var entity in _movers)
        {
            ref var position = ref Registry.Get<GridPosition>(entity);
            ref var mover = ref Registry.Get<GridMover>(entity);
            ref var transform = ref Registry.Get<Transform>(entity);

            var from = grid.CellToCenter(mover.From);
            var to = grid.CellToCenter(mover.To);

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
            Registry.Remove<GridMover>(entity);
        }
    }
}
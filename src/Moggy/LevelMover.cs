using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct LevelTransform
{
    public Cell Position;
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
            .Include<LevelTransform>()
            .Include<LevelMover>()
            .Include<Sprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();

        _completed.Clear();
        foreach (var entity in _movers)
        {
            ref var levelTransform = ref Registry.Get<LevelTransform>(entity);
            ref var levelMover = ref Registry.Get<LevelMover>(entity);
            ref var sprite = ref Registry.Get<Sprite>(entity);

            var from = level.CellToCenter(levelMover.From);
            var to = level.CellToCenter(levelMover.To);

            levelMover.Progress += levelMover.Speed * time.Delta;
            if (levelMover.Progress >= 1f)
            {
                levelTransform.Position = levelMover.To;
                sprite.Transform.Position = to;
                _completed.Add(entity);
                continue;
            }

            sprite.Transform.Position = Vector2.Lerp(from, to, levelMover.Progress);
        }

        foreach (var entity in _completed)
        {
            Registry.Remove<LevelMover>(entity);
        }
    }
}
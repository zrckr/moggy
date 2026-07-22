using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct NavigationTarget : ITag;

public struct Navigation
{
    private const int TraceLength = 16;

    public const int Unreachable = -1;

    public Cell Target;

    public readonly int[] Distances;

    private readonly List<Cell> _trace;

    public Navigation(Cell target, int[] distances)
    {
        Target = target;
        Distances = distances;
        _trace = new List<Cell>(TraceLength) { target };
    }

    public readonly int GetDistance(in Level level, Cell cell)
    {
        return Distances[(cell.Row * level.Columns) + cell.Column];
    }

    public readonly Cell GetOldestTraceCell()
    {
        return _trace[0];
    }

    public readonly IReadOnlyList<Cell> GetTrace()
    {
        return _trace;
    }

    public bool TryRecord(Cell cell)
    {
        if (_trace[^1] == cell)
        {
            return false;
        }

        if (_trace.Count == TraceLength)
        {
            _trace.RemoveAt(0);
        }

        _trace.Add(cell);
        return true;
    }

    public void ResetTrace(Cell cell)
    {
        _trace.Clear();
        _trace.Add(cell);
    }

    public readonly bool TryGetTraceSuccessor(Cell cell, out Cell successor)
    {
        for (var index = _trace.Count - 2; index >= 0; index--)
        {
            if (_trace[index] != cell)
            {
                continue;
            }

            successor = _trace[index + 1];
            return true;
        }

        successor = default;
        return false;
    }
}

public sealed class NavigationGameSystem : GameSystem, IGameSystemGroupState
{
    private Query _target = null!;

    private Entity _navigationEntity = Entity.Invalid;

    public override void Startup()
    {
        _target = Registry.Query()
            .Include<NavigationTarget>()
            .Include<LevelTransform>()
            .Build();
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var transform = ref Registry.Get<LevelTransform>(_target.Single());
        ref var navigation = ref Registry.Singleton<Navigation>();

        navigation.TryRecord(transform.Position);

        var target = navigation.GetOldestTraceCell();
        if (navigation.Target == target)
        {
            return;
        }

        Rebuild(ref navigation, in level, target);
    }

    public void Enter()
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var transform = ref Registry.Get<LevelTransform>(_target.Single());

        _navigationEntity = Registry.Create(
            new Navigation(transform.Position, new int[level.Rows * level.Columns]));

        ref var navigation = ref Registry.Get<Navigation>(_navigationEntity);
        Rebuild(ref navigation, in level, transform.Position);
    }

    public void Exit()
    {
        Registry.Destroy(_navigationEntity);
        _navigationEntity = Entity.Invalid;
    }

    private static void Rebuild(ref Navigation navigation, in Level level, Cell target)
    {
        // Reset the field before measuring each walkable cell from the player.
        Array.Fill(navigation.Distances, Navigation.Unreachable);

        navigation.Target = target;
        navigation.Distances[(target.Row * level.Columns) + target.Column] = 0;

        var cells = new Queue<Cell>();
        cells.Enqueue(target);

        while (cells.TryDequeue(out var cell))
        {
            var distance = navigation.GetDistance(in level, cell);

            foreach (var direction in Enum.GetValues<FaceDirection>())
            {
                var next = cell + direction;
                if (level.IsWalkable(next) &&
                    navigation.GetDistance(in level, next) == Navigation.Unreachable)
                {
                    navigation.Distances[(next.Row * level.Columns) + next.Column] = distance + 1;
                    cells.Enqueue(next);
                }
            }
        }
    }
}

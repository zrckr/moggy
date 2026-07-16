using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public struct Enemy()
{
    public FaceDirection Direction = FaceDirection.Down;
}

public sealed class EnemySystem : GameSystem
{
    private const int EnemyCount = 4;

    private const float CellMoveDuration = 0.2f;

    private const int SpawnSeed = 0;

    private Query _enemies = null!;

    private Query _target = null!;

    private SpriteAsset _moveSprite = null!;

    private readonly HashSet<Cell> _claimedCells = [];

    public override void Startup()
    {
        _moveSprite = Assets.Load<SpriteAsset>("Divil/Move");

        ref var level = ref Registry.Singleton<Level>();

        var random = new Random(SpawnSeed);
        var spawnCells = new HashSet<Cell>();
        for (var i = 0; i < EnemyCount; i++)
        {
            var origin = new Cell(random.Next(level.Columns), random.Next(level.Rows));
            var startCell = FindNearestUnclaimedWalkableCell(in level, origin, spawnCells);
            spawnCells.Add(startCell);

            Registry.Create(
                new Enemy(),
                new LevelPosition
                {
                    Cell = startCell
                },
                new Transform
                {
                    Position = level.CellToCenter(startCell),
                    Scale = new Vector2(2f)
                },
                new AnimatedSprite
                {
                    Animation = FaceDirection.Down.GetAnimationName(),
                    Sprite = _moveSprite
                });
        }

        _target = Registry.Query()
            .Include<NavigationTarget>()
            .Include<LevelPosition>()
            .Build();

        _enemies = Registry.Query()
            .Include<Enemy>()
            .Include<LevelPosition>()
            .Include<Transform>()
            .Include<AnimatedSprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var navigation = ref Registry.Singleton<Navigation>();
        ref var targetPosition = ref Registry.Get<LevelPosition>(_target.Single());

        // Claim current cells and destinations before planning this frame's moves.
        ClaimOccupiedCells();

        foreach (var entity in _enemies)
        {
            ref var enemy = ref Registry.Get<Enemy>(entity);
            ref var levelPosition = ref Registry.Get<LevelPosition>(entity);
            ref var animated = ref Registry.Get<AnimatedSprite>(entity);

            if (!Registry.Has<LevelMover>(entity))
            {
                var startedMove = false;
                if (level.TryRaycast(levelPosition.Cell, targetPosition.Cell, out var direction))
                {
                    navigation.ResetTrace(targetPosition.Cell);
                    startedMove = TryStartDirectedMove(entity, ref enemy, in level, in levelPosition, direction);
                }

                if (!startedMove &&
                    !TryStartTraceMove(entity, ref enemy, in level, in navigation, in levelPosition))
                {
                    TryStartMove(entity, ref enemy, in level, in navigation, in levelPosition);
                }
            }

            animated.Animation = enemy.Direction.GetAnimationName();
            animated.FlipHorizontal = enemy.Direction.IsAnimationFlipped();
        }
    }

    private void TryStartMove(
        Entity entity,
        ref Enemy enemy,
        in Level level,
        in Navigation navigation,
        in LevelPosition position)
    {
        var distance = navigation.GetDistance(in level, position.Cell);
        if (distance == Navigation.Unreachable)
        {
            return;
        }

        if (position.Cell == navigation.Target)
        {
            return;
        }

        var bestDistance = distance;
        var bestDirection = default(FaceDirection);
        var reverseDirection = enemy.Direction.Opposite();
        var reverseCell = default(Cell);
        var canReverse = false;

        foreach (var direction in Enum.GetValues<FaceDirection>())
        {
            var target = position.Cell + direction;
            if (!level.IsWalkable(target) || _claimedCells.Contains(target))
            {
                continue;
            }

            if (direction == reverseDirection)
            {
                reverseCell = target;
                canReverse = true;
                continue;
            }

            var targetDistance = navigation.GetDistance(in level, target);
            if (targetDistance == Navigation.Unreachable || targetDistance >= bestDistance)
            {
                continue;
            }

            bestDistance = targetDistance;
            bestDirection = direction;
        }

        if (bestDistance != distance)
        {
            StartMove(entity, ref enemy, position.Cell, bestDirection, position.Cell + bestDirection);
            return;
        }

        if (!canReverse)
        {
            return;
        }

        // A dead end or reservation leaves reversing as the only viable route.
        StartMove(entity, ref enemy, position.Cell, reverseDirection, reverseCell);
    }

    private bool TryStartTraceMove(
        Entity entity,
        ref Enemy enemy,
        in Level level,
        in Navigation navigation,
        in LevelPosition position)
    {
        if (!navigation.TryGetTraceSuccessor(position.Cell, out var target) ||
            !level.TryRaycast(position.Cell, target, out var direction))
        {
            return false;
        }

        return TryStartDirectedMove(entity, ref enemy, in level, in position, direction);
    }

    private bool TryStartDirectedMove(
        Entity entity,
        ref Enemy enemy,
        in Level level,
        in LevelPosition position,
        FaceDirection direction)
    {
        if (direction == enemy.Direction.Opposite())
        {
            return false;
        }

        var target = position.Cell + direction;
        if (!level.IsWalkable(target) || _claimedCells.Contains(target))
        {
            return false;
        }

        StartMove(entity, ref enemy, position.Cell, direction, target);
        return true;
    }

    private void StartMove(Entity entity, ref Enemy enemy, Cell from, FaceDirection direction, Cell targetCell)
    {
        enemy.Direction = direction;
        _claimedCells.Add(targetCell);

        Registry.SetDeferred(entity, new LevelMover
        {
            From = from,
            To = targetCell,
            Progress = 0f,
            Speed = 1f / CellMoveDuration
        });
    }

    private void ClaimOccupiedCells()
    {
        _claimedCells.Clear();

        foreach (var entity in _enemies)
        {
            ref var position = ref Registry.Get<LevelPosition>(entity);
            _claimedCells.Add(position.Cell);

            if (!Registry.Has<LevelMover>(entity))
            {
                continue;
            }

            ref var mover = ref Registry.Get<LevelMover>(entity);
            _claimedCells.Add(mover.To);
        }
    }

    private static Cell FindNearestUnclaimedWalkableCell(
        in Level level,
        Cell origin,
        IReadOnlySet<Cell> claimedCells)
    {
        var nearest = default(Cell);
        var nearestDistance = int.MaxValue;
        var found = false;

        for (var row = 0; row < level.Rows; row++)
        {
            for (var column = 0; column < level.Columns; column++)
            {
                var cell = new Cell(column, row);
                if (!level.IsWalkable(cell) || claimedCells.Contains(cell))
                {
                    continue;
                }

                var distance = Math.Abs(cell.Row - origin.Row) + Math.Abs(cell.Column - origin.Column);
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearest = cell;
                nearestDistance = distance;
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException("Level does not have enough walkable cells for enemies.");
        }

        return nearest;
    }
}
using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;

namespace Moggy;

public struct Enemy()
{
    public FaceDirection Direction = FaceDirection.Down;

    public float MovementSpeed;
}

public sealed class EnemySystem : GameSystem
{
    private Query _enemies = null!;

    private Query _target = null!;

    private SpriteAsset _moveSprite = null!;

    private readonly HashSet<Cell> _claimedCells = [];

    public override void Startup()
    {
        ref var level = ref Registry.Singleton<Level>();

        var definition = Assets.LoadJson<EnemyDefinition>("Divil/Definition");
        var spawnCells = new HashSet<Cell>();
        _moveSprite = Assets.Load<SpriteAsset>(definition.MoveSprite);
        var random = new Random(definition.SpawnSeed);

        for (var i = 0; i < definition.Count; i++)
        {
            var origin = new Cell(random.Next(level.Columns), random.Next(level.Rows));
            var startCell = FindNearestUnclaimedWalkableCell(in level, origin, spawnCells);
            spawnCells.Add(startCell);

            Registry.Create(
                new Enemy { MovementSpeed = definition.MovementSpeed },
                new LevelTransform { Position = startCell },
                new Sprite
                {
                    Asset = _moveSprite,
                    Transform = new Transform(level.CellToCenter(startCell), new Vector2(2f), 0f),
                    Animation = new SpriteAnimation(FaceDirection.Down.GetAnimationName())
                });
        }

        _target = Registry.Query()
            .Include<NavigationTarget>()
            .Include<LevelTransform>()
            .Build();

        _enemies = Registry.Query()
            .Include<Enemy>()
            .Include<LevelTransform>()
            .Include<Sprite>()
            .Build();
    }

    public override void Update(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var navigation = ref Registry.Singleton<Navigation>();
        ref var levelTransform = ref Registry.Get<LevelTransform>(_target.Single());

        // Claim current cells and destinations before planning this frame's moves.
        ClaimOccupiedCells();

        foreach (var entity in _enemies)
        {
            ref var enemy = ref Registry.Get<Enemy>(entity);
            ref var transform = ref Registry.Get<LevelTransform>(entity);
            ref var sprite = ref Registry.Get<Sprite>(entity);

            if (!Registry.Has<LevelMover>(entity))
            {
                var startedMove = false;
                if (level.TryRaycast(transform.Position, levelTransform.Position, out var direction))
                {
                    navigation.ResetTrace(levelTransform.Position);
                    startedMove = TryStartDirectedMove(entity, ref enemy, in level, in transform, direction);
                }

                if (!startedMove &&
                    !TryStartTraceMove(entity, ref enemy, in level, in navigation, in transform))
                {
                    TryStartMove(entity, ref enemy, in level, in navigation, in transform);
                }
            }

            sprite.Animation.SetName(enemy.Direction.GetAnimationName());
            sprite.FlipH = enemy.Direction.IsAnimationFlipped();
        }
    }

    private void TryStartMove(
        Entity entity,
        ref Enemy enemy,
        in Level level,
        in Navigation navigation,
        in LevelTransform transform)
    {
        var distance = navigation.GetDistance(in level, transform.Position);
        if (distance == Navigation.Unreachable)
        {
            return;
        }

        if (transform.Position == navigation.Target)
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
            var target = transform.Position + direction;
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
            StartMove(entity, ref enemy, transform.Position, bestDirection, transform.Position + bestDirection);
            return;
        }

        if (!canReverse)
        {
            return;
        }

        // A dead end or reservation leaves reversing as the only viable route.
        StartMove(entity, ref enemy, transform.Position, reverseDirection, reverseCell);
    }

    private bool TryStartTraceMove(
        Entity entity,
        ref Enemy enemy,
        in Level level,
        in Navigation navigation,
        in LevelTransform transform)
    {
        if (!navigation.TryGetTraceSuccessor(transform.Position, out var target) ||
            !level.TryRaycast(transform.Position, target, out var direction))
        {
            return false;
        }

        return TryStartDirectedMove(entity, ref enemy, in level, in transform, direction);
    }

    private bool TryStartDirectedMove(
        Entity entity,
        ref Enemy enemy,
        in Level level,
        in LevelTransform transform,
        FaceDirection direction)
    {
        if (direction == enemy.Direction.Opposite())
        {
            return false;
        }

        var target = transform.Position + direction;
        if (!level.IsWalkable(target) || _claimedCells.Contains(target))
        {
            return false;
        }

        StartMove(entity, ref enemy, transform.Position, direction, target);
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
            Speed = enemy.MovementSpeed
        });
    }

    private void ClaimOccupiedCells()
    {
        _claimedCells.Clear();

        foreach (var entity in _enemies)
        {
            ref var transform = ref Registry.Get<LevelTransform>(entity);
            _claimedCells.Add(transform.Position);

            if (!Registry.Has<LevelMover>(entity))
            {
                continue;
            }

            ref var mover = ref Registry.Get<LevelMover>(entity);
            _claimedCells.Add(mover.To);
        }
    }

    public override void Shutdown()
    {
        _moveSprite.Dispose();
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

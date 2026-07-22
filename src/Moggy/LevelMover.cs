using System.Numerics;
using Foster.Framework;
using Moggy.Ecs;

namespace Moggy;

public struct LevelTransform
{
    public Cell Position;
}

public struct LevelMover(Cell from, Cell to, float speed, Cell? visualWrapTo = null)
{
    public Cell From = from;
    public Cell To = to;
    public Cell? VisualWrapTo = visualWrapTo;
    public float Progress = 0f;
    public float Speed = speed;
}

public sealed class LevelMoverSystem : GameSystem
{
    private Query _movers = null!;

    private Query _camera = null!;

    private CameraFollow? _cameraFollow;

    private readonly List<Entity> _completed = new();

    public override void Startup()
    {
        _movers = Registry.Query()
            .Include<LevelTransform>()
            .Include<LevelMover>()
            .Include<Sprite>()
            .Build();

        _camera = Registry.Query()
            .Include<Camera>()
            .Build();
    }

    public override void Update(Time time)
    {
        RestoreCameraFollow();

        ref var level = ref Registry.Singleton<Level>();

        _completed.Clear();
        foreach (var entity in _movers)
        {
            ref var levelTransform = ref Registry.Get<LevelTransform>(entity);
            ref var levelMover = ref Registry.Get<LevelMover>(entity);
            ref var sprite = ref Registry.Get<Sprite>(entity);

            var from = level.CellToCenter(levelMover.From);
            var to = level.CellToCenter(levelMover.To);
            var moveDistance = levelMover.From.ManhattanDistance(levelMover.To);

            levelMover.Progress += levelMover.Speed * time.Delta / moveDistance;
            if (levelMover.Progress >= 1f)
            {
                CompleteMove(entity, ref levelTransform, ref levelMover, ref sprite, in level);
                continue;
            }

            sprite.Transform.Position = Vector2.Lerp(from, to, levelMover.Progress);
        }

        foreach (var entity in _completed)
        {
            Registry.Remove<LevelMover>(entity);
        }
    }

    private void CompleteMove(
        Entity entity,
        ref LevelTransform transform,
        ref LevelMover mover,
        ref Sprite sprite,
        in Level level)
    {
        var direction = mover.From.DirectionTo(mover.To);
        if (mover.VisualWrapTo is { } teleportDestination)
        {
            // Teleport outside the sibling exit, then move onto its exit tile.
            if (!level.TryGetWrap(mover.From, direction, out var destinationWrap))
            {
                throw new InvalidOperationException("A wrapping move must have generated exit topology.");
            }

            var sourcePosition = sprite.Transform.Position;
            transform.Position = teleportDestination;
            sprite.Transform.Position = level.CellToCenter(teleportDestination);

            if (Registry.Has<Player>(entity))
            {
                StartCameraWrap(
                    sourcePosition,
                    sprite.Transform.Position,
                    destinationWrap.DestinationTeleport.ManhattanDistance(destinationWrap.DestinationExit),
                    mover.Speed);
            }

            mover = new LevelMover(teleportDestination, destinationWrap.DestinationExit, mover.Speed);
            return;
        }

        transform.Position = mover.To;
        sprite.Transform.Position = level.CellToCenter(mover.To);
        if (level.GetTile(mover.To) != Mazegen.Tile.Exit)
        {
            _completed.Add(entity);
            return;
        }

        if (level.TryGetWrap(mover.To, direction, out var sourceWrap))
        {
            mover = new LevelMover(
                mover.To,
                sourceWrap.SourceTeleport,
                mover.Speed,
                sourceWrap.DestinationTeleport);
            return;
        }

        // Entering from outside continues through the sibling exit without input.
        var interior = mover.To + direction.ToPoint2();
        if (!level.IsWalkable(interior))
        {
            throw new InvalidOperationException("An exit entrance must lead to a walkable interior cell.");
        }

        mover = new LevelMover(mover.To, interior, mover.Speed);
    }

    private void StartCameraWrap(Vector2 from, Vector2 to, int travelDistance, float movementSpeed)
    {
        if (movementSpeed <= 0f)
        {
            throw new InvalidOperationException("Camera wrapping requires positive movement speed.");
        }

        var cameraEntity = _camera.Single();
        ref var camera = ref Registry.Get<Camera>(cameraEntity);
        _cameraFollow = Registry.Get<CameraFollow>(cameraEntity);

        var displacement = to - from;
        var duration = TimeSpan.FromSeconds(travelDistance / movementSpeed);

        Registry.RemoveDeferred<CameraFollow>(cameraEntity);
        Registry.SetDeferred(cameraEntity, new CameraPath
        {
            From = camera.Position,
            To = camera.Position + displacement,
            Duration = duration
        });
    }

    private void RestoreCameraFollow()
    {
        if (!_cameraFollow.HasValue)
        {
            return;
        }

        var cameraEntity = _camera.Single();
        if (Registry.Has<CameraPath>(cameraEntity))
        {
            return;
        }

        Registry.Set(cameraEntity, _cameraFollow.Value);
        _cameraFollow = null;
    }
}

using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Moggy.Mazegen;

namespace Moggy;

public sealed record LevelProperties
{
    public int TilingRows { get; init; }

    public int TilingColumns { get; init; }

    public int CellSize { get; init; }

    public int TargetScore { get; init; }

    public int StartingLives { get; init; }

    public int BugScore { get; init; }

    public int EnemyScore { get; init; }

    public float AbilityOdds { get; init; }

    public int AbilityTrigger { get; init; }

    public TimeSpan RespawnInvincibility { get; init; }

    public required ChaosProperties Chaos { get; init; }

    public required MazeProperties Maze { get; init; }
}

public readonly record struct Cell(int Column, int Row)
{
    public FaceDirection DirectionTo(Cell to)
    {
        if (Column < to.Column) return FaceDirection.Right;
        if (Column > to.Column) return FaceDirection.Left;
        if (Row < to.Row) return FaceDirection.Down;
        if (Row > to.Row) return FaceDirection.Up;
        throw new InvalidOperationException("A level move must change cells.");
    }

    public int ManhattanDistance(Cell to)
    {
        return Math.Abs(Column - to.Column) + Math.Abs(Row - to.Row);
    }

    public static Cell operator +(Cell cell, Point2 direction)
    {
        return new Cell(cell.Column + direction.X, cell.Row + direction.Y);
    }
}

public readonly record struct LevelMove(Cell To, Cell? VisualWrapTo = null);

public readonly record struct LevelWrap(Cell SourceTeleport, Cell DestinationTeleport, Cell DestinationExit);

public struct LevelDebug()
{
    public bool ShowTiles = false;
}

public readonly struct Level
{
    public readonly Maze Maze;

    public readonly int CellWidth;

    public readonly int CellHeight;

    private readonly Dictionary<(Cell Cell, FaceDirection Direction), LevelWrap> _wraps;

    public Level(Maze maze, int cellWidth, int cellHeight)
    {
        Maze = maze;
        CellWidth = cellWidth;
        CellHeight = cellHeight;
        _wraps = new Dictionary<(Cell, FaceDirection), LevelWrap>();
        foreach (var pair in maze.ExitPairs)
        {
            AddWrap(_wraps, pair.First.FirstLane, pair.First.OutwardDirection, pair.Second.FirstLane);
            AddWrap(_wraps, pair.First.SecondLane, pair.First.OutwardDirection, pair.Second.SecondLane);
            AddWrap(_wraps, pair.Second.FirstLane, pair.Second.OutwardDirection, pair.First.FirstLane);
            AddWrap(_wraps, pair.Second.SecondLane, pair.Second.OutwardDirection, pair.First.SecondLane);
        }
    }

    public int Rows => Maze.Rows;

    public int Columns => Maze.Columns;

    public int Width => Columns * CellWidth;

    public int Height => Rows * CellHeight;

    public bool Contains(Cell cell)
    {
        return cell is { Row: >= 0, Column: >= 0 } &&
               cell.Column < Columns &&
               cell.Row < Rows;
    }

    public Tile GetTile(Cell cell)
    {
        return Maze[cell.Row, cell.Column];
    }

    public bool IsWalkable(Cell cell)
    {
        return Contains(cell) && GetTile(cell) is Tile.Empty or Tile.Floor or Tile.Exit;
    }

    public bool TryResolveMove(Cell origin, FaceDirection direction, out LevelMove move)
    {
        var adjacent = origin + direction.ToPoint2();
        if (IsWalkable(adjacent))
        {
            move = new LevelMove(adjacent);
            return true;
        }

        if (_wraps.TryGetValue((origin, direction), out var wrap))
        {
            var destination = wrap.DestinationExit + direction.ToPoint2();
            if (!IsWalkable(destination))
            {
                throw new InvalidOperationException("A paired exit must lead to a walkable interior cell.");
            }

            move = new LevelMove(destination, adjacent);
            return true;
        }

        move = default;
        return false;
    }

    public bool TryGetWrap(Cell exit, FaceDirection outwardDirection, out LevelWrap wrap)
    {
        return _wraps.TryGetValue((exit, outwardDirection), out wrap);
    }

    private static void AddWrap(
        Dictionary<(Cell Cell, FaceDirection Direction), LevelWrap> wraps,
        MazeExitLane source,
        Direction direction,
        MazeExitLane destination)
    {
        wraps.Add(
            (ToMazeCell(source.ExitCell), direction.ToFaceDirection()),
            new LevelWrap(
                ToMazeCell(source.TeleportCell),
                ToMazeCell(destination.TeleportCell),
                ToMazeCell(destination.ExitCell)));
    }

    private static Cell ToMazeCell(Point2 point)
    {
        return new Cell(point.Y, point.X);
    }

    public bool TryRaycast(Cell origin, Cell target, out FaceDirection direction)
    {
        if (origin.Row == target.Row && origin.Column != target.Column)
        {
            direction = origin.Column < target.Column
                ? FaceDirection.Right
                : FaceDirection.Left;
        }
        else if (origin.Column == target.Column && origin.Row != target.Row)
        {
            direction = origin.Row < target.Row
                ? FaceDirection.Down
                : FaceDirection.Up;
        }
        else
        {
            direction = default;
            return false;
        }

        for (var cell = origin + direction.ToPoint2(); cell != target; cell += direction.ToPoint2())
        {
            if (!IsWalkable(cell))
            {
                return false;
            }
        }

        return IsWalkable(target);
    }

    public Cell FindNearestWalkableCell(Cell origin)
    {
        if (IsWalkable(origin))
        {
            return origin;
        }

        var best = new Cell(0, 0);
        var bestDistance = int.MaxValue;
        var found = false;
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var cell = new Cell(column, row);
                if (!IsWalkable(cell))
                {
                    continue;
                }

                var distance = Math.Abs(cell.Row - origin.Row) + Math.Abs(cell.Column - origin.Column);
                if (distance >= bestDistance)
                {
                    continue;
                }

                best = cell;
                bestDistance = distance;
                found = true;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException("Level has no walkable cells.");
        }

        return best;
    }

    public Vector2 CellToWorld(Cell cell)
    {
        return new Vector2(
            (-Width / 2f) + (cell.Column * CellWidth),
            (-Height / 2f) + (cell.Row * CellHeight));
    }

    public Vector2 CellToCenter(Cell cell)
    {
        return CellToWorld(cell) + (new Vector2(CellWidth, CellHeight) * 0.5f);
    }
}

public sealed class LevelGameSystem : GameSystem, IGameSystemGroupState
{
    private LevelProperties _properties = null!;

    private ImageAsset _wall = null!;

    private ImageAsset _debugTiles = null!;

    private Subtexture[] _tileSprites = [];

    private Entity _levelEntity = Entity.Invalid;

    public override void Startup()
    {
        _properties = Assets.LoadJson<LevelProperties>("LevelProperties");
        _wall = Assets.Load<ImageAsset>("GridWall");
        _debugTiles = Assets.Load<ImageAsset>("Level/DebugTiles");
        Registry.Create(new LevelDebug());

        var tileCount = Enum.GetValues<Tile>().Length;
        if (_debugTiles.Width % tileCount != 0)
        {
            throw new InvalidOperationException("The debug tile strip width must be divisible by the tile count.");
        }

        var tileWidth = _debugTiles.Width / tileCount;
        _tileSprites = new Subtexture[tileCount];
        for (var index = 0; index < tileCount; index++)
        {
            _tileSprites[index] = new Subtexture(
                _debugTiles.Texture,
                new Rect(index * tileWidth, 0, tileWidth, _debugTiles.Height));
        }
    }

    public override void Render(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();
        ref var debug = ref Registry.Singleton<LevelDebug>();

        for (var row = 0; row < level.Rows; row++)
        {
            for (var column = 0; column < level.Columns; column++)
            {
                var cell = new Cell(column, row);
                var tile = level.GetTile(cell);
                if (tile is Tile.Wall or Tile.Corner)
                {
                    Batcher.Image(_wall.Texture, level.CellToWorld(cell), Color.White);
                }

                if (debug.ShowTiles)
                {
                    // Draw the generated tile type over the normal level presentation.
                    var tileSprite = _tileSprites[(int)tile];
                    var scale = new Vector2(
                        level.CellWidth / tileSprite.Width,
                        level.CellHeight / tileSprite.Height);

                    Batcher.Image(tileSprite, level.CellToWorld(cell), Vector2.Zero, scale, 0f,
                        Color.White with { A = 64 });
                }
            }
        }
    }

    public override void Shutdown()
    {
        _wall.Dispose();
        _debugTiles.Dispose();
    }

    public void Enter()
    {
        var region = new List<Point2>();
        for (var row = 0; row < _properties.TilingRows; row++)
        {
            for (var column = 0; column < _properties.TilingColumns; column++)
            {
                region.Add(new Point2(row, column));
            }
        }

        var maze = MazeGenerator.Generate(region, _properties.Maze);
        _levelEntity = Registry.Create(
            new Level(maze, _properties.CellSize, _properties.CellSize));
    }

    public void Exit()
    {
        Registry.Destroy(_levelEntity);
        _levelEntity = Entity.Invalid;
    }
}

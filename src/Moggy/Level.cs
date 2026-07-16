using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Moggy.Mazegen;

namespace Moggy;

public readonly record struct Cell(int Column, int Row)
{
    public static Cell operator+(Cell cell, FaceDirection direction)
    {
        var point2 = direction.ToPoint2();
        return new Cell(cell.Column + point2.X, cell.Row + point2.Y);
    }
}

public struct Level
{
    public required Maze Maze;

    public required int CellWidth;

    public required int CellHeight;

    public readonly int Rows => Maze.Rows;

    public readonly int Columns => Maze.Columns;

    public readonly int Width => Columns * CellWidth;

    public readonly int Height => Rows * CellHeight;

    public readonly bool Contains(Cell cell)
    {
        return cell is { Row: >= 0, Column: >= 0 } &&
               cell.Column < Columns &&
               cell.Row < Rows;
    }

    public readonly Tile GetTile(Cell cell)
    {
        return Maze[cell.Row, cell.Column];
    }

    public readonly bool IsWalkable(Cell cell)
    {
        return Contains(cell) && GetTile(cell) is Tile.Empty or Tile.Floor;
    }

    public readonly bool TryRaycast(Cell origin, Cell target, out FaceDirection direction)
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

        for (var cell = origin + direction; cell != target; cell += direction)
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

    public readonly Vector2 CellToWorld(Cell cell)
    {
        return new Vector2(
            -Width / 2f + cell.Column * CellWidth,
            -Height / 2f + cell.Row * CellHeight);
    }

    public readonly Vector2 CellToCenter(Cell cell)
    {
        return CellToWorld(cell) + new Vector2(CellWidth, CellHeight) * 0.5f;
    }
}

public sealed class LevelSystem : GameSystem
{
    private const int TilingRows = 10;

    private const int TilingColumns = 10;

    private const int CellSize = 16;

    private Query _level = null!;

    private ImageAsset _wall = null!;

    public override void Startup()
    {
        var maze = MazeGenerator.Generate(
            GenerateRegion(),
            new MazeGeneratorOptions());

        var level = Registry.Create();
        Registry.Set(level, new Level
        {
            Maze = maze,
            CellWidth = CellSize,
            CellHeight = CellSize
        });

        _wall = Assets.Load<ImageAsset>("GridWall");
        _level = Registry.Query()
            .Include<Level>()
            .Build();
    }

    public override void Render(Time time)
    {
        var entity = _level.Single();
        ref var level = ref Registry.Get<Level>(entity);

        for (var row = 0; row < level.Rows; row++)
        {
            for (var column = 0; column < level.Columns; column++)
            {
                var cell = new Cell(column, row);
                switch (level.Maze[row, column])
                {
                    case Tile.Wall:
                    case Tile.Corner:
                        if (level.Contains(cell))
                        {
                            Batcher.Image(_wall.Texture, level.CellToWorld(cell), Color.White);
                        }
                        break;
                }
            }
        }
    }

    public override void Shutdown()
    {
        _wall.Dispose();
    }

    private static IReadOnlyCollection<Point2> GenerateRegion()
    {
        var region = new List<Point2>();
        for (var row = 0; row < TilingRows; row++)
        {
            for (var column = 0; column < TilingColumns; column++)
            {
                region.Add(new Point2(row, column));
            }
        }

        return region;
    }
}
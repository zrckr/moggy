using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
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

public readonly struct Level(Maze maze, int cellWidth, int cellHeight)
{
    public readonly Maze Maze = maze;

    public readonly int CellWidth = cellWidth;

    public readonly int CellHeight = cellHeight;

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
        return Contains(cell) && GetTile(cell) is Tile.Empty or Tile.Floor;
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

    public Vector2 CellToWorld(Cell cell)
    {
        return new Vector2(
            -Width / 2f + cell.Column * CellWidth,
            -Height / 2f + cell.Row * CellHeight);
    }

    public Vector2 CellToCenter(Cell cell)
    {
        return CellToWorld(cell) + new Vector2(CellWidth, CellHeight) * 0.5f;
    }
}

public sealed class LevelSystem : GameSystem
{
    private ImageAsset _wall = null!;

    public override void Startup()
    {
        var definition = Assets.LoadJson<LevelDefinition>("LevelDefinition");
        var maze = MazeGenerator.Generate(
            GenerateRegion(definition.TilingRows, definition.TilingColumns),
            Assets.LoadJson<MazeDefinition>("MazeDefinition"));

        Registry.Create(new Level(maze, definition.CellSize, definition.CellSize));

        _wall = Assets.Load<ImageAsset>("GridWall");
    }

    public override void Render(Time time)
    {
        ref var level = ref Registry.Singleton<Level>();

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

    private static IReadOnlyCollection<Point2> GenerateRegion(int tilingRows, int tilingColumns)
    {
        var region = new List<Point2>();
        for (var row = 0; row < tilingRows; row++)
        {
            for (var column = 0; column < tilingColumns; column++)
            {
                region.Add(new Point2(row, column));
            }
        }

        return region;
    }
}

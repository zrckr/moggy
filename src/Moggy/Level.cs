using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Moggy.Mazegen;

namespace Moggy;

public struct Level
{
    public required Maze Maze;

    public required int CellWidth;

    public required int CellHeight;

    public readonly int Rows => Maze.Rows;

    public readonly int Columns => Maze.Columns;

    public readonly int Width => Columns * CellWidth;

    public readonly int Height => Rows * CellHeight;

    public readonly bool Contains(Point2 cell)
    {
        return cell is { X: >= 0, Y: >= 0 } &&
               cell.X < Columns &&
               cell.Y < Rows;
    }

    public Tile GetTile(Point2 cell)
    {
        return Maze[cell.Y, cell.X];
    }

    public bool IsWalkable(Point2 cell)
    {
        return Contains(cell) && GetTile(cell) is Tile.Empty or Tile.Floor;
    }

    public Point2 FindNearestWalkableCell(Point2 origin)
    {
        if (IsWalkable(origin))
        {
            return origin;
        }

        var best = new Point2(0, 0);
        var bestDistance = int.MaxValue;
        var found = false;
        for (var row = 0; row < Rows; row++)
        {
            for (var column = 0; column < Columns; column++)
            {
                var cell = new Point2(column, row);
                if (!IsWalkable(cell))
                {
                    continue;
                }

                var distance = Math.Abs(cell.X - origin.X) + Math.Abs(cell.Y - origin.Y);
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

    public readonly Vector2 CellToWorld(Point2 cell)
    {
        return new Vector2(
            -Width / 2f + cell.X * CellWidth,
            -Height / 2f + cell.Y * CellHeight);
    }

    public Vector2 CellToCenter(Point2 cell)
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
                switch (level.Maze[row, column])
                {
                    case Tile.Wall:
                    case Tile.Corner:
                        DrawCell(level, _wall, new Point2(column, row), Color.White);
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

    private void DrawCell(in Level level, ImageAsset asset, Point2 cell, Color color)
    {
        if (!level.Contains(cell))
        {
            return;
        }

        Batcher.Image(asset.Texture, level.CellToWorld(cell), color);
    }
}
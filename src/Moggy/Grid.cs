using System.Numerics;
using Foster.Framework;
using Moggy.Assets;
using Moggy.Ecs;
using Moggy.Mazegen;

namespace Moggy;

public readonly struct Grid(int rows, int columns, int cellWidth, int cellHeight)
{
    public readonly int Rows = rows;
    public readonly int Columns = columns;
    public readonly int CellWidth = cellWidth;
    public readonly int CellHeight = cellHeight;

    public int Width => Columns * CellWidth;

    public int Height => Rows * CellHeight;

    public bool Contains(Point2 cell)
    {
        return cell is { X: >= 0, Y: >= 0 } &&
               cell.X < Columns &&
               cell.Y < Rows;
    }

    public Vector2 CellToWorld(Point2 cell)
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

public sealed class GridSystem : GameSystem
{
    private const int TilingRows = 10;

    private const int TilingColumns = 10;

    private Query _grid = null!;

    private ImageAsset _gridCell = null!;

    private ImageAsset _gridWall = null!;

    public override void Startup()
    {
        _gridCell = Assets.Load<ImageAsset>("GridCell");
        _gridWall = Assets.Load<ImageAsset>("GridWall");
        _grid = Registry.Query()
            .Include<Grid>()
            .Build();

        var entity = _grid.Single();
        Registry.Set(entity, MazeGenerator.Generate(
            GenerateRegion(),
            new MazeGeneratorOptions()));

        _grid = Registry.Query()
            .Include<Grid>()
            .Include<Maze>()
            .Build();
    }

    public override void Render(Time time)
    {
        var entity = _grid.Single();
        ref var grid = ref Registry.Get<Grid>(entity);
        ref var maze = ref Registry.Get<Maze>(entity);

        var originRow = (grid.Rows - maze.Rows) / 2;
        var originColumn = (grid.Columns - maze.Columns) / 2;

        for (var row = 0; row < maze.Rows; row++)
        {
            for (var column = 0; column < maze.Columns; column++)
            {
                switch (maze[row, column])
                {
                    case Tile.Wall:
                    case Tile.Corner:
                        DrawCell(grid, _gridWall, originRow + row, originColumn + column, Color.White);
                        break;
                }
            }
        }
    }

    public override void Shutdown()
    {
        _gridWall.Dispose();
        _gridCell.Dispose();
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

    private void DrawCell(in Grid grid, ImageAsset asset, int row, int column, Color color)
    {
        if (row < 0 || row >= grid.Rows || column < 0 || column >= grid.Columns)
        {
            return;
        }

        Batcher.Image(asset.Texture, grid.CellToWorld(new Point2(column, row)), color);
    }
}

namespace Moggy.Mazegen;

using Foster.Framework;

internal static class MazeMaterializer
{
    private const int Scale = 3;

    private const int FloorSize = 2;

    public static Maze Materialize(
        PieceTopology topology,
        WallPlan walls,
        IReadOnlyList<OuterExitPair> exitPairs,
        int exitTravelDistance)
    {
        var rows = (topology.Rows * Scale) + 1;
        var columns = (topology.Columns * Scale) + 1;
        var tiles = new Tile[rows * columns];

        for (var row = 0; row <= topology.Rows; row++)
        {
            for (var column = 0; column <= topology.Columns; column++)
            {
                Set(tiles, columns, row * Scale, column * Scale, Tile.Corner);
            }
        }

        for (var row = 0; row < topology.Rows; row++)
        {
            for (var column = 0; column < topology.Columns; column++)
            {
                var tileRow = row * Scale;
                var tileColumn = column * Scale;

                for (var y = 0; y < FloorSize; y++)
                {
                    for (var x = 0; x < FloorSize; x++)
                    {
                        Set(tiles, columns, tileRow + 1 + y, tileColumn + 1 + x, Tile.Floor);
                    }
                }
            }
        }

        for (var row = 0; row <= topology.Rows; row++)
        {
            for (var column = 0; column < topology.Columns; column++)
            {
                if (!walls.Horizontal[row, column])
                {
                    continue;
                }

                Set(tiles, columns, row * Scale, (column * Scale) + 1, Tile.Wall);
                Set(tiles, columns, row * Scale, (column * Scale) + 2, Tile.Wall);
            }
        }

        for (var row = 0; row < topology.Rows; row++)
        {
            for (var column = 0; column <= topology.Columns; column++)
            {
                if (!walls.Vertical[row, column])
                {
                    continue;
                }

                Set(tiles, columns, (row * Scale) + 1, column * Scale, Tile.Wall);
                Set(tiles, columns, (row * Scale) + 2, column * Scale, Tile.Wall);
            }
        }

        var materializedExitPairs = new MazeExitPair[exitPairs.Count];
        for (var index = 0; index < exitPairs.Count; index++)
        {
            var pair = exitPairs[index];
            materializedExitPairs[index] = new MazeExitPair(
                MaterializeExit(tiles, rows, columns, pair.First, exitTravelDistance),
                MaterializeExit(tiles, rows, columns, pair.Second, exitTravelDistance));
        }

        return new Maze(rows, columns, tiles, materializedExitPairs);
    }

    private static MazeExit MaterializeExit(
        Tile[] tiles,
        int rows,
        int columns,
        OuterWall wall,
        int exitTravelDistance)
    {
        var (first, second) = wall.Side switch
        {
            Direction.North => (
                new Point2(0, (wall.Cell.Y * Scale) + 1),
                new Point2(0, (wall.Cell.Y * Scale) + 2)),
            Direction.South => (
                new Point2(rows - 1, (wall.Cell.Y * Scale) + 1),
                new Point2(rows - 1, (wall.Cell.Y * Scale) + 2)),
            Direction.West => (
                new Point2((wall.Cell.X * Scale) + 1, 0),
                new Point2((wall.Cell.X * Scale) + 2, 0)),
            Direction.East => (
                new Point2((wall.Cell.X * Scale) + 1, columns - 1),
                new Point2((wall.Cell.X * Scale) + 2, columns - 1)),
            _ => throw new ArgumentOutOfRangeException(nameof(wall), wall, null)
        };

        Set(tiles, columns, first.X, first.Y, Tile.Exit);
        Set(tiles, columns, second.X, second.Y, Tile.Exit);

        var outward = wall.Side.ToPoint2() * exitTravelDistance;
        return new MazeExit(
            new MazeExitLane(first, first + outward),
            new MazeExitLane(second, second + outward),
            wall.Side);
    }

    private static void Set(Tile[] tiles, int columns, int row, int column, Tile tile)
    {
        tiles[(row * columns) + column] = tile;
    }
}

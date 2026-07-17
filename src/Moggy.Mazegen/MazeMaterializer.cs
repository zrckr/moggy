namespace Moggy.Mazegen;

internal static class MazeMaterializer
{
    private const int Scale = 3;

    private const int FloorSize = 2;

    public static Maze Materialize(PieceTopology topology, WallPlan walls)
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

        return new Maze(rows, columns, tiles);
    }

    private static void Set(Tile[] tiles, int columns, int row, int column, Tile tile)
    {
        tiles[(row * columns) + column] = tile;
    }
}

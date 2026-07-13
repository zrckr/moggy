using Foster.Framework;

namespace Moggy.Mazegen;

public readonly struct Maze(int rows, int columns, Tile[] tiles)
{
    public readonly int Rows = rows;

    public readonly int Columns = columns;

    public Tile this[int row, int column] => tiles[(row * Columns) + column];

    public Tile this[Point2 cell] => tiles[(cell.X * Columns) + cell.Y];
}
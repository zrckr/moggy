using Foster.Framework;

namespace Moggy.Mazegen;

public readonly record struct MazeExitLane(Point2 ExitCell, Point2 TeleportCell);

public readonly record struct MazeExit(MazeExitLane FirstLane, MazeExitLane SecondLane, Direction OutwardDirection);

public readonly record struct MazeExitPair(MazeExit First, MazeExit Second);

public readonly struct Maze(int rows, int columns, Tile[] tiles, IReadOnlyList<MazeExitPair> exitPairs)
{
    public readonly int Rows = rows;

    public readonly int Columns = columns;

    public readonly IReadOnlyList<MazeExitPair> ExitPairs = exitPairs;

    public Tile this[int row, int column] => tiles[(row * Columns) + column];

    public Tile this[Point2 cell] => tiles[(cell.X * Columns) + cell.Y];
}

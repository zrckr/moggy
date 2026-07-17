using Foster.Framework;

namespace Moggy.Mazegen;

internal enum Direction
{
    North,
    East,
    South,
    West
}

internal static class DirectionExtensions
{
    public static Point2 ToPoint2(this Direction direction)
    {
        return direction switch
        {
            Direction.North => new Point2(-1, 0),
            Direction.East => new Point2(0, 1),
            Direction.South => new Point2(1, 0),
            Direction.West => new Point2(0, -1),
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static Direction Opposite(this Direction direction)
    {
        return (Direction)((int)(direction + 2) % 4);
    }
}

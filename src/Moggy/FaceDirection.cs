using System.Numerics;
using Foster.Framework;

namespace Moggy;

public enum FaceDirection
{
    Down,
    Left,
    Up,
    Right
}

public static class FaceDirectionExtensions
{
    public static Vector2 ToVector2(this FaceDirection faceDirection)
    {
        return faceDirection switch
        {
            FaceDirection.Up => new Vector2(0, -1),
            FaceDirection.Right => new Vector2(1, 0),
            FaceDirection.Down => new Vector2(0, 1),
            FaceDirection.Left => new Vector2(-1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(faceDirection), faceDirection, null)
        };
    }

    public static Point2 ToPoint2(this FaceDirection faceDirection)
    {
        return faceDirection switch
        {
            FaceDirection.Up => new Point2(0, -1),
            FaceDirection.Right => new Point2(1, 0),
            FaceDirection.Down => new Point2(0, 1),
            FaceDirection.Left => new Point2(-1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(faceDirection), faceDirection, null)
        };
    }

    public static FaceDirection Opposite(this FaceDirection faceDirection)
    {
        return faceDirection switch
        {
            FaceDirection.Up => FaceDirection.Down,
            FaceDirection.Right => FaceDirection.Left,
            FaceDirection.Down => FaceDirection.Up,
            FaceDirection.Left => FaceDirection.Right,
            _ => throw new ArgumentOutOfRangeException(nameof(faceDirection), faceDirection, null)
        };
    }

    public static FaceDirection? ToFaceDirection(this Vector2 vector)
    {
        if (vector.X < 0f) return FaceDirection.Left;
        if (vector.X > 0f) return FaceDirection.Right;
        if (vector.Y < 0f) return FaceDirection.Up;
        if (vector.Y > 0f) return FaceDirection.Down;
        return null;
    }

    public static FaceDirection? ToFaceDirection(this VirtualStick move)
    {
        if (move.PressedLeft) return FaceDirection.Left;
        if (move.PressedRight) return FaceDirection.Right;
        if (move.PressedUp) return FaceDirection.Up;
        if (move.PressedDown) return FaceDirection.Down;
        return null;
    }

    public static FaceDirection ToFaceDirection(this Mazegen.Direction direction)
    {
        return direction switch
        {
            Mazegen.Direction.North => FaceDirection.Up,
            Mazegen.Direction.East => FaceDirection.Right,
            Mazegen.Direction.South => FaceDirection.Down,
            Mazegen.Direction.West => FaceDirection.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
        };
    }

    public static string GetAnimationName(this FaceDirection faceDirection)
    {
        return faceDirection switch
        {
            FaceDirection.Up => "up",
            FaceDirection.Down => "down",
            FaceDirection.Left or FaceDirection.Right => "side",
            _ => throw new ArgumentOutOfRangeException(nameof(faceDirection), faceDirection, null)
        };
    }

    public static string GetAnimationName2D(this FaceDirection faceDirection)
    {
        return faceDirection switch
        {
            FaceDirection.Up or FaceDirection.Down => "vertical",
            FaceDirection.Left or FaceDirection.Right => "horizontal",
            _ => throw new ArgumentOutOfRangeException(nameof(faceDirection), faceDirection, null)
        };
    }

    public static bool IsAnimationFlipped(this FaceDirection faceDirection)
    {
        return faceDirection == FaceDirection.Right;
    }


}

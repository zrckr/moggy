using System.Numerics;

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

    public static FaceDirection ToFaceDirection(this Vector2 vector)
    {
        if (vector.X < 0f) return FaceDirection.Left;
        if (vector.X > 0f) return FaceDirection.Right;
        if (vector.Y < 0f) return FaceDirection.Up;
        if (vector.Y > 0f) return FaceDirection.Down;
        throw new ArgumentOutOfRangeException(nameof(vector), vector, null);
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

    public static bool IsAnimationFlipped(this FaceDirection faceDirection)
    {
        return faceDirection == FaceDirection.Right;
    }
}
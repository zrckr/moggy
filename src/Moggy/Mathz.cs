using System.Numerics;
using System.Runtime.CompilerServices;
using Foster.Framework;

namespace Moggy;

public static class Mathz
{
    private const float Half = 0.5f;

    public const int TileSize = 8;

    public const int CellSize = TileSize * 2;

    public const int VirtualWidth = TileSize * 40;

    public const int VirtualHeight = TileSize * 30;

    public static readonly RectInt ViewportSize = new(0, CellSize * 2, VirtualWidth, VirtualHeight - (CellSize * 3));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp01(double value)
    {
        return (float)Math.Clamp(value, 0f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Difference(this Rect bounds, in Rect rect)
    {
        return new Vector2(
            Difference(bounds.Left, bounds.Right, rect.Left, rect.Right),
            Difference(bounds.Top, bounds.Bottom, rect.Top, rect.Bottom)
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Difference(float boundsMinimum, float boundsMaximum, float rectMinimum, float rectMaximum)
    {
        var boundsSize = boundsMaximum - boundsMinimum;
        var rectSize = rectMaximum - rectMinimum;
        if (rectSize >= boundsSize)
        {
            var boundsCenter = (boundsMinimum + boundsMaximum) * Half;
            var rectCenter = (rectMinimum + rectMaximum) * Half;
            return rectCenter - boundsCenter;
        }

        if (rectMinimum < boundsMinimum)
        {
            return rectMinimum - boundsMinimum;
        }

        if (rectMaximum > boundsMaximum)
        {
            return rectMaximum - boundsMaximum;
        }

        return 0f;
    }
}

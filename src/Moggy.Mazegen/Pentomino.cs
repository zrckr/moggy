using Foster.Framework;

namespace Moggy.Mazegen;

internal enum Pentomino
{
    F,
    I,
    L,
    P,
    N,
    T,
    U,
    V,
    W,
    X,
    Y,
    Z
}

internal static class PentominoExtensions
{
    private static Point2[] Base(this Pentomino pentomino)
    {
        return pentomino switch
        {
            Pentomino.F => new Point2[] { new(0, 1), new(0, 2), new(1, 0), new(1, 1), new(2, 1) },
            Pentomino.I => new Point2[] { new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(4, 0) },
            Pentomino.L => new Point2[] { new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(3, 1) },
            Pentomino.P => new Point2[] { new(0, 0), new(0, 1), new(1, 0), new(1, 1), new(2, 0) },
            Pentomino.N => new Point2[] { new(0, 0), new(1, 0), new(1, 1), new(2, 1), new(3, 1) },
            Pentomino.T => new Point2[] { new(0, 0), new(0, 1), new(0, 2), new(1, 1), new(2, 1) },
            Pentomino.U => new Point2[] { new(0, 0), new(0, 2), new(1, 0), new(1, 1), new(1, 2) },
            Pentomino.V => new Point2[] { new(0, 0), new(1, 0), new(2, 0), new(2, 1), new(2, 2) },
            Pentomino.W => new Point2[] { new(0, 0), new(1, 0), new(1, 1), new(2, 1), new(2, 2) },
            Pentomino.X => new Point2[] { new(0, 1), new(1, 0), new(1, 1), new(1, 2), new(2, 1) },
            Pentomino.Y => new Point2[] { new(0, 0), new(1, 0), new(2, 0), new(3, 0), new(1, 1) },
            Pentomino.Z => new Point2[] { new(0, 0), new(0, 1), new(1, 1), new(2, 1), new(2, 2) },
            _ => throw new ArgumentOutOfRangeException(nameof(pentomino), pentomino, null)
        };
    }

    public static Color Color(this Pentomino pentomino)
    {
        return pentomino switch
        {
            Pentomino.F => new Color(0.90f, 0.22f, 0.22f, 1f),
            Pentomino.I => new Color(0.18f, 0.62f, 0.92f, 1f),
            Pentomino.L => new Color(0.32f, 0.78f, 0.42f, 1f),
            Pentomino.P => new Color(0.96f, 0.72f, 0.20f, 1f),
            Pentomino.N => new Color(0.76f, 0.42f, 0.92f, 1f),
            Pentomino.T => new Color(0.12f, 0.76f, 0.72f, 1f),
            Pentomino.U => new Color(0.96f, 0.42f, 0.20f, 1f),
            Pentomino.V => new Color(0.74f, 0.84f, 0.20f, 1f),
            Pentomino.W => new Color(0.42f, 0.48f, 0.94f, 1f),
            Pentomino.X => new Color(0.92f, 0.30f, 0.62f, 1f),
            Pentomino.Y => new Color(0.34f, 0.68f, 0.84f, 1f),
            Pentomino.Z => new Color(0.72f, 0.56f, 0.36f, 1f),
            _ => throw new ArgumentOutOfRangeException(nameof(pentomino), pentomino, null)
        };
    }

    public static HashSet<Point2>[] Orientations(this Pentomino pentomino)
    {
        var result = new HashSet<HashSet<Point2>>(PointSetComparer.Instance);
        var current = NormalizedBase(pentomino.Base());

        for (var rotation = 0; rotation < 4; rotation++)
        {
            result.Add(current);
            result.Add(NormalizedBase(current.Select(cell => new Point2(cell.X, -cell.Y))));
            current = NormalizedBase(current.Select(cell => new Point2(cell.Y, -cell.X)));
        }

        return result.ToArray();
    }

    private static HashSet<Point2> NormalizedBase(IEnumerable<Point2> values)
    {
        var points = values.ToArray();
        var row0 = points.Min(cell => cell.X);
        var column0 = points.Min(cell => cell.Y);
        return points
            .Select(cell => new Point2(cell.X - row0, cell.Y - column0))
            .ToHashSet();
    }
}

internal sealed record PentominoPlacement(Pentomino Pentomino, IReadOnlySet<Point2> Cells);

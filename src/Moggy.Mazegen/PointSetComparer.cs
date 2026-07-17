using Foster.Framework;

namespace Moggy.Mazegen;

internal sealed class PointSetComparer : IEqualityComparer<HashSet<Point2>>
{
    public static readonly PointSetComparer Instance = new();

    public bool Equals(HashSet<Point2>? x, HashSet<Point2>? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        return x is not null && y is not null && x.SetEquals(y);
    }

    public int GetHashCode(HashSet<Point2> obj)
    {
        var hashCode = new HashCode();
        foreach (var cell in obj.OrderBy(cell => cell.X).ThenBy(cell => cell.Y))
        {
            hashCode.Add(cell);
        }

        return hashCode.ToHashCode();
    }
}

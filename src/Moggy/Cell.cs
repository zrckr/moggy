namespace Moggy;

public struct Cell(int column, int row) : IEquatable<Cell>
{
    public int Column = column;

    public int Row = row;

    public Cell() : this(0, 0) { }

    public readonly int ManhattanDistance(Cell other)
    {
        return Math.Abs(Column - other.Column) + Math.Abs(Row - other.Row);
    }

    public readonly override string ToString()
    {
        return $"({Column}, {Row})";
    }

    public bool Equals(Cell other)
    {
        return Column == other.Column && Row == other.Row;
    }

    public override bool Equals(object? obj)
    {
        return obj is Cell other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Column, Row);
    }

    public static bool operator ==(Cell left, Cell right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Cell left, Cell right)
    {
        return !(left == right);
    }
}

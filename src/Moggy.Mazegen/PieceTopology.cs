using Foster.Framework;

namespace Moggy.Mazegen;

internal readonly record struct PieceEdge(int First, int Second)
{
    public static PieceEdge Between(int first, int second)
    {
        return first < second
            ? new PieceEdge(first, second)
            : new PieceEdge(second, first);
    }
}

internal readonly record struct PieceConnection(int First, int Second, CellSegment Segment);

internal readonly record struct CellSegment(Point2 First, Point2 Second);

internal sealed class PieceTopology
{
    public int Rows { get; }

    public int Columns { get; }

    public int[,] PieceGrid { get; } // Maze generation treats Point2.X as row and Point2.Y as column.

    public IReadOnlyDictionary<PieceEdge, IReadOnlyList<CellSegment>> Boundaries { get; }

    public int PieceCount => _tiling.Count;

    private readonly Dictionary<int, HashSet<int>> _graphSets;

    private readonly IReadOnlyList<PentominoPlacement> _tiling;

    private PieceTopology(int rows, int columns,
        IReadOnlyList<PentominoPlacement> tiling,
        int[,] pieceGrid,
        IReadOnlyDictionary<PieceEdge, IReadOnlyList<CellSegment>> boundaries,
        Dictionary<int, HashSet<int>> graphSets)
    {
        Rows = rows;
        Columns = columns;
        PieceGrid = pieceGrid;
        Boundaries = boundaries;
        _tiling = tiling;
        _graphSets = graphSets;
    }


    public static PieceTopology Build(IReadOnlyList<PentominoPlacement> tiling)
    {
        var (minRow, minColumn, rows, columns) = MeasureBounds(tiling.SelectMany(piece => piece.Cells));
        var pieceGrid = new int[rows, columns];

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                pieceGrid[row, column] = -1;
            }
        }

        var normalizedTiling = new PentominoPlacement[tiling.Count];
        for (var pieceId = 0; pieceId < tiling.Count; pieceId++)
        {
            var cells = tiling[pieceId].Cells
                .Select(cell => new Point2(cell.X - minRow, cell.Y - minColumn))
                .ToHashSet();
            normalizedTiling[pieceId] = new PentominoPlacement(tiling[pieceId].Pentomino, cells);

            foreach (var cell in cells)
            {
                pieceGrid[cell.X, cell.Y] = pieceId;
            }
        }

        var boundaries = FindSharedBoundaries(rows, columns, pieceGrid);
        var graph = GraphFromBoundaries(normalizedTiling.Length, boundaries.Keys);

        return new PieceTopology(rows, columns, normalizedTiling, pieceGrid, boundaries, graph);
    }

    private IReadOnlySet<int> BorderPieces()
    {
        var result = new HashSet<int>();
        for (var pieceId = 0; pieceId < _tiling.Count; pieceId++)
        {
            if (_tiling[pieceId].Cells.Any(cell =>
                    cell.X == 0 ||
                    cell.X == Rows - 1 ||
                    cell.Y == 0 ||
                    cell.Y == Columns - 1))
            {
                result.Add(pieceId);
            }
        }

        return result;
    }

    private Dictionary<int, int> GraphDistances(IEnumerable<int> starts)
    {
        var distance = Enumerable.Range(0, PieceCount).ToDictionary(piece => piece, _ => -1);
        var queue = new Queue<int>();

        foreach (var start in starts)
        {
            distance[start] = 0;
            queue.Enqueue(start);
        }

        while (queue.Count > 0)
        {
            var piece = queue.Dequeue();
            foreach (var neighbor in _graphSets[piece])
            {
                if (distance[neighbor] != -1)
                {
                    continue;
                }

                distance[neighbor] = distance[piece] + 1;
                queue.Enqueue(neighbor);
            }
        }

        return distance;
    }

    public int ChooseInsidePiece(Random random)
    {
        var depth = GraphDistances(BorderPieces());
        var maximum = depth.Values.Max();
        var choices = depth
            .Where(entry => entry.Value == maximum)
            .Select(entry => entry.Key)
            .ToArray();

        return choices[random.Next(choices.Length)];
    }

    public Direction SegmentDirection(int piece, CellSegment segment)
    {
        var first = segment.First;
        var second = segment.Second;
        int rowDelta;
        int columnDelta;

        if (PieceGrid[first.X, first.Y] == piece)
        {
            rowDelta = second.X - first.X;
            columnDelta = second.Y - first.Y;
        }
        else
        {
            rowDelta = first.X - second.X;
            columnDelta = first.Y - second.Y;
        }

        foreach (var direction in Enum.GetValues<Direction>())
        {
            var delta = direction.ToPoint2();
            if (delta.X == rowDelta && delta.Y == columnDelta)
            {
                return direction;
            }
        }

        throw new InvalidOperationException("Segment does not touch the requested piece.");
    }

    public Dictionary<int, HashSet<int>> FullGraphCopy()
    {
        return _graphSets.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ToHashSet());
    }

    private static IReadOnlyDictionary<PieceEdge, IReadOnlyList<CellSegment>> FindSharedBoundaries(
        int rows,
        int columns,
        int[,] grid)
    {
        var result = new Dictionary<PieceEdge, List<CellSegment>>();

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                AddBoundary(row, column, row + 1, column);
                AddBoundary(row, column, row, column + 1);
            }
        }

        return result.ToDictionary(
            entry => entry.Key,
            entry => (IReadOnlyList<CellSegment>)entry.Value);

        void AddBoundary(int firstRow, int firstColumn, int secondRow, int secondColumn)
        {
            if (secondRow >= rows || secondColumn >= columns)
            {
                return;
            }

            var first = grid[firstRow, firstColumn];
            var second = grid[secondRow, secondColumn];
            if (first == second)
            {
                return;
            }

            var edge = PieceEdge.Between(first, second);
            if (!result.TryGetValue(edge, out var segments))
            {
                segments = new List<CellSegment>();
                result.Add(edge, segments);
            }

            segments.Add(new CellSegment(
                new Point2(firstRow, firstColumn),
                new Point2(secondRow, secondColumn)));
        }
    }

    private static Dictionary<int, HashSet<int>> GraphFromBoundaries(int pieceCount, IEnumerable<PieceEdge> boundaries)
    {
        var graph = Enumerable.Range(0, pieceCount)
            .ToDictionary(piece => piece, _ => new HashSet<int>());

        foreach (var boundary in boundaries)
        {
            graph[boundary.First].Add(boundary.Second);
            graph[boundary.Second].Add(boundary.First);
        }

        return graph;
    }

    private static TilingBounds MeasureBounds(IEnumerable<Point2> cells)
    {
        var points = cells.ToArray();
        var minRow = points.Min(cell => cell.X);
        var maxRow = points.Max(cell => cell.X);
        var minColumn = points.Min(cell => cell.Y);
        var maxColumn = points.Max(cell => cell.Y);

        return new TilingBounds(minRow, minColumn, maxRow - minRow + 1, maxColumn - minColumn + 1);
    }
}

internal readonly record struct TilingBounds(int MinRow, int MinColumn, int Height, int Width);

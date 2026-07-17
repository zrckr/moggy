using Foster.Framework;

namespace Moggy.Mazegen;

public sealed record MazeDefinition
{
    public int Seed { get; init; } = 17;

    public float Erosion { get; init; } = 1f / 3f;

    public int Exits { get; init; } = 4;

    public int MaxHallway { get; init; } = 3;

    public int MaxDeadEnd { get; init; } = 3;

    public int MaxRepairs { get; init; } = 12;

    public int ExtraLoops { get; init; } = 0;

    public float Temperature { get; init; } = 0.6f;
}

public static class MazeGenerator
{
    private const int WallErosionSeedSalt = 0xE20510;

    private const float DeadEndRepairBaseScore = 3f;

    public static Maze Generate(IReadOnlyCollection<Point2> region, MazeDefinition options)
    {
        var tiling = PentominoTiling.Generate(region, new Random(options.Seed));
        if (tiling.Count == 0)
        {
            throw new InvalidOperationException("Unable to generate pentomino tiling.");
        }

        var topology = PieceTopology.Build(tiling);
        var walls = GenerateWalls(topology, options, options.Seed);

        return MazeMaterializer.Materialize(topology, walls);
    }

    private static WallPlan GenerateWalls(PieceTopology topology, MazeDefinition options, int seed)
    {
        // The maze is first built as a pentomino-piece graph, then converted into unit-cell walls.
        var random = new Random(seed);
        var connections = new SpanningTreeBuilder(topology, options, random).Build();
        connections = RepairLongDeadEnds(topology, connections, options, random);
        connections = AddUsefulLoops(topology, connections, options.ExtraLoops, random);
        var exits = ChooseExits(topology, options.Exits, random);
        var walls = BuildWalls(topology, connections, exits, options, new Random(seed ^ WallErosionSeedSalt));

        if (OuterOpeningCount(topology, walls) != exits.Count)
        {
            throw new InvalidOperationException("Outer boundary changed somewhere other than an exit.");
        }

        if (!AllCellsReachable(topology, walls))
        {
            throw new InvalidOperationException("Unit-cell reachability validation failed.");
        }

        return walls;
    }

    private static List<PieceConnection> RepairLongDeadEnds(
        PieceTopology topology,
        IReadOnlyList<PieceConnection> connections,
        MazeDefinition options,
        Random random)
    {
        var repaired = connections.ToList();
        var fullGraph = topology.FullGraphCopy();

        for (var repair = 0; repair < options.MaxRepairs; repair++)
        {
            var graph = ConnectionGraph(topology.PieceCount, repaired);
            var overlongDeadEnds = Enumerable.Range(0, topology.PieceCount)
                .Where(piece => graph[piece].Count == 1 && TerminalDepth(graph, piece) > options.MaxDeadEnd)
                .ToArray();

            if (overlongDeadEnds.Length == 0)
            {
                break;
            }

            var used = UsedPieceEdges(repaired);
            var candidates = new List<(float Score, PieceConnection Connection)>();

            foreach (var leaf in overlongDeadEnds)
            {
                var chain = new List<int> { leaf };
                int? previous = null;
                var current = leaf;

                while (graph[current].Count <= 2)
                {
                    var onward = graph[current]
                        .Where(node => previous is null || node != previous.Value)
                        .ToArray();
                    if (onward.Length == 0)
                    {
                        break;
                    }

                    previous = current;
                    current = onward[0];
                    chain.Add(current);

                    if (graph[current].Count != 2)
                    {
                        break;
                    }
                }

                for (var index = 0; index < chain.Count - 1; index++)
                {
                    var piece = chain[index];
                    foreach (var neighbor in fullGraph[piece])
                    {
                        var edge = PieceEdge.Between(piece, neighbor);
                        if (used.Contains(edge) || chain.Contains(neighbor))
                        {
                            continue;
                        }

                        var segment = Choose(topology.Boundaries[edge], random);
                        candidates.Add((
                            DeadEndRepairBaseScore - index + random.NextSingle(),
                            new PieceConnection(piece, neighbor, segment)));
                    }
                }
            }

            if (candidates.Count == 0)
            {
                break;
            }

            repaired.Add(candidates.MaxBy(candidate => candidate.Score).Connection);
        }

        return repaired;
    }

    private static List<PieceConnection> AddUsefulLoops(
        PieceTopology topology,
        IReadOnlyList<PieceConnection> connections,
        int count,
        Random random)
    {
        var result = connections.ToList();

        for (var loop = 0; loop < Math.Max(0, count); loop++)
        {
            var graph = ConnectionGraph(topology.PieceCount, result);
            var used = UsedPieceEdges(result);
            var candidates = new List<(float Score, PieceConnection Connection)>();

            foreach (var (edge, segments) in topology.Boundaries)
            {
                if (used.Contains(edge))
                {
                    continue;
                }

                var distance = ShortestPath(graph, edge.First, edge.Second);
                candidates.Add((
                    distance + random.NextSingle(),
                    new PieceConnection(edge.First, edge.Second, Choose(segments, random))));
            }

            if (candidates.Count == 0)
            {
                break;
            }

            result.Add(candidates.MaxBy(candidate => candidate.Score).Connection);
        }

        return result;
    }

    private static WallPlan BuildWalls(
        PieceTopology topology,
        IReadOnlyList<PieceConnection> connections,
        IReadOnlyList<OuterWall> exits,
        MazeDefinition options,
        Random random)
    {
        // Wall arrays use true for closed walls and false for openings.
        var (horizontal, vertical) = MakeWallArrays(topology);

        foreach (var connection in connections)
        {
            OpenSegment(horizontal, vertical, connection.Segment);
        }

        ErodeInternalWalls(topology, horizontal, vertical, options.Erosion, random);

        foreach (var exit in exits)
        {
            OpenExit(horizontal, vertical, exit);
        }

        return new WallPlan(horizontal, vertical);
    }

    private static (bool[,] Horizontal, bool[,] Vertical) MakeWallArrays(PieceTopology topology)
    {
        var horizontal = new bool[topology.Rows + 1, topology.Columns];
        var vertical = new bool[topology.Rows, topology.Columns + 1];

        for (var column = 0; column < topology.Columns; column++)
        {
            horizontal[0, column] = true;
            horizontal[topology.Rows, column] = true;
        }

        for (var row = 0; row < topology.Rows; row++)
        {
            vertical[row, 0] = true;
            vertical[row, topology.Columns] = true;
        }

        for (var row = 0; row < topology.Rows; row++)
        {
            for (var column = 0; column < topology.Columns - 1; column++)
            {
                if (topology.PieceGrid[row, column] != topology.PieceGrid[row, column + 1])
                {
                    vertical[row, column + 1] = true;
                }
            }
        }

        for (var row = 0; row < topology.Rows - 1; row++)
        {
            for (var column = 0; column < topology.Columns; column++)
            {
                if (topology.PieceGrid[row, column] != topology.PieceGrid[row + 1, column])
                {
                    horizontal[row + 1, column] = true;
                }
            }
        }

        return (horizontal, vertical);
    }

    private static void OpenSegment(bool[,] horizontal, bool[,] vertical, CellSegment segment)
    {
        if (segment.First.X == segment.Second.X)
        {
            vertical[segment.First.X, Math.Max(segment.First.Y, segment.Second.Y)] = false;
        }
        else
        {
            horizontal[Math.Max(segment.First.X, segment.Second.X), segment.First.Y] = false;
        }
    }

    private static void ErodeInternalWalls(
        PieceTopology topology,
        bool[,] horizontal,
        bool[,] vertical,
        float erosion,
        Random random)
    {
        var candidates = ClosedInternalPieceWalls(topology, horizontal, vertical).ToArray();
        random.Shuffle(candidates);

        var removeCount = (int)Math.Round(Math.Clamp(erosion, 0f, 1f) * candidates.Length);
        foreach (var candidate in candidates.Take(removeCount))
        {
            if (candidate.Kind == WallAxis.Horizontal)
            {
                horizontal[candidate.Row, candidate.Column] = false;
            }
            else
            {
                vertical[candidate.Row, candidate.Column] = false;
            }
        }
    }

    private static IEnumerable<WallCell> ClosedInternalPieceWalls(
        PieceTopology topology,
        bool[,] horizontal,
        bool[,] vertical)
    {
        for (var row = 0; row < topology.Rows; row++)
        {
            for (var column = 0; column < topology.Columns - 1; column++)
            {
                if (topology.PieceGrid[row, column] != topology.PieceGrid[row, column + 1] &&
                    vertical[row, column + 1])
                {
                    yield return new WallCell(WallAxis.Vertical, row, column + 1);
                }
            }
        }

        for (var row = 0; row < topology.Rows - 1; row++)
        {
            for (var column = 0; column < topology.Columns; column++)
            {
                if (topology.PieceGrid[row, column] != topology.PieceGrid[row + 1, column] &&
                    horizontal[row + 1, column])
                {
                    yield return new WallCell(WallAxis.Horizontal, row + 1, column);
                }
            }
        }
    }

    private static void OpenExit(bool[,] horizontal, bool[,] vertical, OuterWall wall)
    {
        switch (wall.Side)
        {
            case Direction.North:
                horizontal[0, wall.Cell.Y] = false;
                break;
            case Direction.South:
                horizontal[horizontal.GetLength(0) - 1, wall.Cell.Y] = false;
                break;
            case Direction.West:
                vertical[wall.Cell.X, 0] = false;
                break;
            case Direction.East:
                vertical[wall.Cell.X, vertical.GetLength(1) - 1] = false;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(wall), wall, null);
        }
    }

    private static IReadOnlyList<OuterWall> ChooseExits(PieceTopology topology, int count, Random random)
    {
        var ordered = OuterWallCandidates(topology)
            .OrderBy(wall => PerimeterPosition(topology, wall))
            .ToArray();
        count = Math.Max(0, Math.Min(count, ordered.Length));
        if (count == 0)
        {
            return Array.Empty<OuterWall>();
        }

        var exits = new List<OuterWall>();
        for (var sector = 0; sector < count; sector++)
        {
            var start = (int)Math.Round(sector * ordered.Length / (float)count);
            var end = (int)Math.Round((sector + 1) * ordered.Length / (float)count);
            var sectorWalls = ordered[start..Math.Max(start + 1, end)];
            var usedPieces = exits.Select(exit => exit.Piece).ToHashSet();
            var preferred = sectorWalls
                .Where(wall => !usedPieces.Contains(wall.Piece))
                .ToArray();

            exits.Add(Choose(preferred.Length > 0 ? preferred : sectorWalls, random));
        }

        return exits
            .OrderBy(wall => PerimeterPosition(topology, wall))
            .ToArray();
    }

    private static IEnumerable<OuterWall> OuterWallCandidates(PieceTopology topology)
    {
        for (var row = 0; row < topology.Rows; row++)
        {
            yield return new OuterWall(topology.PieceGrid[row, 0], new Point2(row, 0), Direction.West);
            yield return new OuterWall(
                topology.PieceGrid[row, topology.Columns - 1],
                new Point2(row, topology.Columns - 1),
                Direction.East);
        }

        for (var column = 0; column < topology.Columns; column++)
        {
            yield return new OuterWall(topology.PieceGrid[0, column], new Point2(0, column), Direction.North);
            yield return new OuterWall(
                topology.PieceGrid[topology.Rows - 1, column],
                new Point2(topology.Rows - 1, column),
                Direction.South);
        }
    }

    private static float PerimeterPosition(PieceTopology topology, OuterWall wall)
    {
        return wall.Side switch
        {
            Direction.West => wall.Cell.X + 0.5f,
            Direction.South => topology.Rows + wall.Cell.Y + 0.5f,
            Direction.East => topology.Rows + topology.Columns + (topology.Rows - wall.Cell.X - 0.5f),
            Direction.North => (2f * topology.Rows) + topology.Columns + (topology.Columns - wall.Cell.Y - 0.5f),
            _ => throw new ArgumentOutOfRangeException(nameof(wall), wall, null)
        };
    }

    private static bool AllCellsReachable(PieceTopology topology, WallPlan plan)
    {
        var start = new Point2(0, 0);
        var seen = new HashSet<Point2> { start };
        var queue = new Queue<Point2>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            Add(new Point2(cell.X - 1, cell.Y), cell.X > 0 && !plan.Horizontal[cell.X, cell.Y]);
            Add(new Point2(cell.X + 1, cell.Y), cell.X + 1 < topology.Rows && !plan.Horizontal[cell.X + 1, cell.Y]);
            Add(new Point2(cell.X, cell.Y - 1), cell.Y > 0 && !plan.Vertical[cell.X, cell.Y]);
            Add(new Point2(cell.X, cell.Y + 1), cell.Y + 1 < topology.Columns && !plan.Vertical[cell.X, cell.Y + 1]);
        }

        return seen.Count == topology.Rows * topology.Columns;

        void Add(Point2 next, bool open)
        {
            if (open && seen.Add(next))
            {
                queue.Enqueue(next);
            }
        }
    }

    private static int OuterOpeningCount(PieceTopology topology, WallPlan plan)
    {
        var count = 0;
        for (var column = 0; column < topology.Columns; column++)
        {
            count += plan.Horizontal[0, column] ? 0 : 1;
            count += plan.Horizontal[topology.Rows, column] ? 0 : 1;
        }

        for (var row = 0; row < topology.Rows; row++)
        {
            count += plan.Vertical[row, 0] ? 0 : 1;
            count += plan.Vertical[row, topology.Columns] ? 0 : 1;
        }

        return count;
    }

    private static Dictionary<int, HashSet<int>> ConnectionGraph(
        int pieceCount,
        IEnumerable<PieceConnection> connections)
    {
        var graph = Enumerable.Range(0, pieceCount)
            .ToDictionary(piece => piece, _ => new HashSet<int>());

        foreach (var connection in connections)
        {
            graph[connection.First].Add(connection.Second);
            graph[connection.Second].Add(connection.First);
        }

        return graph;
    }

    private static int TerminalDepth(Dictionary<int, HashSet<int>> graph, int leaf)
    {
        if (graph[leaf].Count != 1)
        {
            return 0;
        }

        int? previous = null;
        var current = leaf;
        var depth = 0;

        while (true)
        {
            var onward = graph[current]
                .Where(node => previous is null || node != previous.Value)
                .ToArray();

            if ((current != leaf && graph[current].Count != 2) || onward.Length == 0)
            {
                return depth;
            }

            previous = current;
            current = onward[0];
            depth++;
        }
    }

    private static int ShortestPath(Dictionary<int, HashSet<int>> graph, int start, int target)
    {
        if (start == target)
        {
            return 0;
        }

        var queue = new Queue<int>();
        var distance = new Dictionary<int, int> { [start] = 0 };
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var piece = queue.Dequeue();
            foreach (var neighbor in graph[piece])
            {
                if (distance.ContainsKey(neighbor))
                {
                    continue;
                }

                distance[neighbor] = distance[piece] + 1;
                if (neighbor == target)
                {
                    return distance[neighbor];
                }

                queue.Enqueue(neighbor);
            }
        }

        return -1;
    }

    private static HashSet<PieceEdge> UsedPieceEdges(IEnumerable<PieceConnection> connections)
    {
        return connections
            .Select(connection => PieceEdge.Between(connection.First, connection.Second))
            .ToHashSet();
    }

    private static T Choose<T>(IReadOnlyList<T> values, Random random)
    {
        return values[random.Next(values.Count)];
    }
}

internal sealed record WallPlan(bool[,] Horizontal, bool[,] Vertical);

internal readonly record struct OuterWall(int Piece, Point2 Cell, Direction Side);

internal readonly record struct WallCell(WallAxis Kind, int Row, int Column);

internal enum WallAxis
{
    Horizontal,
    Vertical
}

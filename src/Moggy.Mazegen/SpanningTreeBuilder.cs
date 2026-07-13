namespace Moggy.Mazegen;

internal sealed class SpanningTreeBuilder
{
    private const float ConnectionScoreJitter = 0.45f;

    private const float DirectionChangeBonus = 1f;

    private const float StraightRunPenalty = 1.1f;

    private const float LongHallwayPenalty = 9f;

    private const float TwoWayJunctionBonus = 0.8f;

    private const float ThinBranchPenalty = 0.3f;

    private const float CrowdedJunctionPenalty = 0.8f;

    private const float FrontierNeighborBonus = 0.18f;

    private const double SoftmaxExponentLimit = 40.0;

    private readonly PieceTopology _topology;

    private readonly MazeGeneratorOptions _options;

    private readonly Random _random;

    private readonly Dictionary<int, HashSet<int>> _fullGraph;

    private readonly Dictionary<int, HashSet<int>> _openGraph;

    private readonly HashSet<int> _reached;

    private readonly Dictionary<int, Direction?> _entryDirection;

    private readonly Dictionary<int, int> _straightRun;

    private readonly List<PieceConnection> _connections = [];

    public SpanningTreeBuilder(PieceTopology topology, MazeGeneratorOptions options, Random random)
    {
        _topology = topology;
        _options = options;
        _random = random;
        _fullGraph = topology.FullGraphCopy();
        _openGraph = Enumerable.Range(0, topology.PieceCount).ToDictionary(piece => piece, _ => new HashSet<int>());
        _entryDirection = Enumerable.Range(0, topology.PieceCount).ToDictionary(piece => piece, _ => (Direction?)null);
        _straightRun = Enumerable.Range(0, topology.PieceCount).ToDictionary(piece => piece, _ => 0);
        _reached = [topology.ChooseInsidePiece(random)];
    }

    public List<PieceConnection> Build()
    {
        while (_reached.Count < _topology.PieceCount)
        {
            var scoredEdges = FindFrontierConnections();
            if (scoredEdges.Count == 0)
            {
                throw new InvalidOperationException("Unable to connect pentomino topology.");
            }

            var withinLimit = scoredEdges
                .Where(item => item.NextRun <= _options.MaxHallway)
                .ToArray();
            var chosen = WeightedChoice(
                withinLimit.Length > 0 ? withinLimit : scoredEdges.ToArray(),
                _options.Temperature);

            AddConnection(chosen);
        }

        return _connections;
    }

    private List<ScoredConnection> FindFrontierConnections()
    {
        var scoredEdges = new List<ScoredConnection>();

        foreach (var (edge, segments) in _topology.Boundaries)
        {
            if (_reached.Contains(edge.First) == _reached.Contains(edge.Second))
            {
                continue;
            }

            var parent = _reached.Contains(edge.First) ? edge.First : edge.Second;
            var child = _reached.Contains(edge.First) ? edge.Second : edge.First;
            ScoredConnection? bestSegment = null;

            foreach (var segment in segments)
            {
                var candidate = ScoreConnection(parent, child, segment);
                if (bestSegment is null || candidate.Score > bestSegment.Value.Score)
                {
                    bestSegment = candidate;
                }
            }

            if (bestSegment is not null)
            {
                scoredEdges.Add(bestSegment.Value);
            }
        }

        return scoredEdges;
    }

    private ScoredConnection ScoreConnection(int parent, int child, CellSegment segment)
    {
        var outgoing = _topology.SegmentDirection(parent, segment);
        var parentEntry = _entryDirection[parent];
        var continuesStraight = parentEntry is not null && outgoing == parentEntry.Value.Opposite();
        var nextRun = continuesStraight ? _straightRun[parent] + 1 : 0;
        var parentDegree = _openGraph[parent].Count;
        var unreachedNeighborCount = _fullGraph[child].Count(neighbor => !_reached.Contains(neighbor));

        var score = (_random.NextSingle() * ConnectionScoreJitter * 2f) - ConnectionScoreJitter;
        score += parentEntry is not null && !continuesStraight ? DirectionChangeBonus : 0f;
        score -= StraightRunPenalty * nextRun;
        score -= LongHallwayPenalty * Math.Max(0, nextRun - _options.MaxHallway);
        score += parentDegree == 2 ? TwoWayJunctionBonus : 0f;
        score -= parentDegree == 1 ? ThinBranchPenalty : 0f;
        score -= parentDegree >= 4 ? CrowdedJunctionPenalty : 0f;
        score += FrontierNeighborBonus * unreachedNeighborCount;

        return new ScoredConnection(
            score,
            new PieceConnection(parent, child, segment),
            nextRun);
    }

    private void AddConnection(ScoredConnection chosen)
    {
        _connections.Add(chosen.Connection);
        _openGraph[chosen.Connection.First].Add(chosen.Connection.Second);
        _openGraph[chosen.Connection.Second].Add(chosen.Connection.First);
        _reached.Add(chosen.Connection.Second);
        _entryDirection[chosen.Connection.Second] = _topology.SegmentDirection(chosen.Connection.Second, chosen.Connection.Segment);
        _straightRun[chosen.Connection.Second] = chosen.NextRun;
    }

    private ScoredConnection WeightedChoice(ScoredConnection[] scoredItems, float temperature)
    {
        if (temperature <= 0)
        {
            return scoredItems.MaxBy(item => item.Score);
        }

        var highest = scoredItems.Max(item => item.Score);
        var weights = scoredItems
            .Select(item => Math.Exp(Math.Clamp(
                (item.Score - highest) / temperature,
                -SoftmaxExponentLimit,
                SoftmaxExponentLimit)))
            .ToArray();
        var total = weights.Sum();
        var selected = _random.NextDouble() * total;

        for (var index = 0; index < scoredItems.Length; index++)
        {
            selected -= weights[index];
            if (selected <= 0)
            {
                return scoredItems[index];
            }
        }

        return scoredItems[^1];
    }

    private readonly record struct ScoredConnection(float Score, PieceConnection Connection, int NextRun);
}
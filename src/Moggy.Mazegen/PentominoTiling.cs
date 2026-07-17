using Foster.Framework;

namespace Moggy.Mazegen;

internal static class PentominoTiling
{
    public static IReadOnlyList<PentominoPlacement> Generate(IEnumerable<Point2> cells, Random random)
    {
        var region = cells.ToHashSet();
        if (region.Count % 5 != 0 || !ComponentsHavePentominoArea(region))
        {
            return [];
        }

        var placements = AllPlacements(region);
        var placementsByCell = region.ToDictionary(cell => cell, _ => new List<int>());

        for (var placementId = 0; placementId < placements.Count; placementId++)
        {
            foreach (var cell in placements[placementId].Cells)
            {
                placementsByCell[cell].Add(placementId);
            }
        }

        var selected = new List<int>();

        return Search(region)
            ? selected.Select(placementId => placements[placementId]).ToArray()
            : [];

        bool Search(HashSet<Point2> uncovered)
        {
            if (uncovered.Count == 0)
            {
                return true;
            }

            // Exact-cover backtracking: try the uncovered cell with the fewest legal placements first.
            var uncoveredCells = uncovered.ToArray();
            random.Shuffle(uncoveredCells);

            List<int>? tightestCellOptions = null;
            foreach (var cell in uncoveredCells)
            {
                var placementOptions = placementsByCell[cell]
                    .Where(placementId => placements[placementId].Cells.IsSubsetOf(uncovered))
                    .ToList();

                if (placementOptions.Count == 0)
                {
                    return false;
                }

                if (tightestCellOptions is null || placementOptions.Count < tightestCellOptions.Count)
                {
                    tightestCellOptions = placementOptions;

                    if (tightestCellOptions.Count == 1)
                    {
                        break;
                    }
                }
            }

            if (tightestCellOptions is null)
            {
                return false;
            }

            var shuffledPlacementOptions = tightestCellOptions.ToArray();
            random.Shuffle(shuffledPlacementOptions);

            foreach (var placementId in shuffledPlacementOptions)
            {
                var remaining = uncovered.Except(placements[placementId].Cells).ToHashSet();

                if (remaining.Count > 0 && !ComponentsHavePentominoArea(remaining))
                {
                    continue;
                }

                selected.Add(placementId);

                if (Search(remaining))
                {
                    return true;
                }

                selected.RemoveAt(selected.Count - 1);
            }

            return false;
        }
    }

    private static IReadOnlyList<PentominoPlacement> AllPlacements(HashSet<Point2> region)
    {
        var result = new List<PentominoPlacement>();
        if (region.Count == 0)
        {
            return result;
        }

        var minRow = region.Min(cell => cell.X);
        var maxRow = region.Max(cell => cell.X);
        var minColumn = region.Min(cell => cell.Y);
        var maxColumn = region.Max(cell => cell.Y);

        foreach (var pentomino in Enum.GetValues<Pentomino>())
        {
            foreach (var shape in pentomino.Orientations())
            {
                var height = shape.Max(cell => cell.X) + 1;
                var width = shape.Max(cell => cell.Y) + 1;

                for (var top = minRow; top <= maxRow - height + 1; top++)
                {
                    for (var left = minColumn; left <= maxColumn - width + 1; left++)
                    {
                        var cells = shape
                            .Select(cell => new Point2(top + cell.X, left + cell.Y))
                            .ToHashSet();
                        if (cells.IsSubsetOf(region))
                        {
                            result.Add(new PentominoPlacement(pentomino, cells));
                        }
                    }
                }
            }
        }

        return result;
    }

    private static bool ComponentsHavePentominoArea(HashSet<Point2> cells)
    {
        var unseen = cells.ToHashSet();
        var stack = new Stack<Point2>();

        while (unseen.Count > 0)
        {
            var start = unseen.First();
            unseen.Remove(start);
            stack.Push(start);

            var size = 1;
            while (stack.Count > 0)
            {
                var cell = stack.Pop();
                foreach (var neighbor in Neighbors(cell))
                {
                    if (!unseen.Remove(neighbor))
                    {
                        continue;
                    }

                    stack.Push(neighbor);
                    size++;
                }
            }

            if (size % 5 != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static IEnumerable<Point2> Neighbors(Point2 cell)
    {
        yield return new Point2(cell.X - 1, cell.Y);
        yield return new Point2(cell.X + 1, cell.Y);
        yield return new Point2(cell.X, cell.Y - 1);
        yield return new Point2(cell.X, cell.Y + 1);
    }
}

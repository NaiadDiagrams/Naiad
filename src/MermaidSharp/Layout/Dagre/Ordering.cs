namespace MermaidSharp;

internal static class Ordering
{
    const int MaxIterations = 24;

    public static void Run(LayoutGraph graph)
    {
        graph.BuildRanks();
        InitializeOrder(graph);

        var bestCrossings = CountCrossings(graph);
        var bestOrders = SaveOrders(graph);

        for (int i = 0; i < MaxIterations && bestCrossings > 0; i++)
        {
            // Alternate between sweeping down and up
            if (i % 2 == 0)
            {
                SweepDown(graph);
            }
            else
            {
                SweepUp(graph);
            }

            var crossings = CountCrossings(graph);
            if (crossings < bestCrossings)
            {
                bestCrossings = crossings;
                bestOrders = SaveOrders(graph);
            }
        }

        RestoreOrders(graph, bestOrders);
        graph.UpdateOrderInRanks();
    }

    static void InitializeOrder(LayoutGraph graph)
    {
        for (int r = 0; r < graph.Ranks.Length; r++)
        {
            var nodesInRank = graph.Ranks[r];
            for (int i = 0; i < nodesInRank.Count; i++)
            {
                nodesInRank[i].Order = i;
            }
        }
    }

    static void SweepDown(LayoutGraph graph)
    {
        for (int r = 1; r < graph.Ranks.Length; r++)
        {
            OrderByMedian(graph, r, true);
        }
    }

    static void SweepUp(LayoutGraph graph)
    {
        for (int r = graph.Ranks.Length - 2; r >= 0; r--)
        {
            OrderByMedian(graph, r, false);
        }
    }

    static void OrderByMedian(LayoutGraph graph, int rank, bool useInEdges)
    {
        var nodesInRank = graph.Ranks[rank];
        var positions = new Dictionary<string, double>();

        foreach (var node in nodesInRank)
        {
            var neighbors = useInEdges
                ? graph.GetPredecessors(node.Id).ToList()
                : graph.GetSuccessors(node.Id).ToList();

            if (neighbors.Count == 0)
            {
                positions[node.Id] = node.Order;
            }
            else
            {
                var neighborOrders = neighbors.Select(n => (double)n.Order).OrderBy(x => x).ToList();
                positions[node.Id] = Median(neighborOrders);
            }
        }

        // Sort by median position, maintaining stability for equal positions
        var sortedNodes = nodesInRank
            .OrderBy(n => positions[n.Id])
            .ThenBy(n => n.Order)
            .ToList();

        for (int i = 0; i < sortedNodes.Count; i++)
        {
            sortedNodes[i].Order = i;
        }

        graph.Ranks[rank] = sortedNodes;
    }

    static double Median(List<double> values)
    {
        if (values.Count == 0) return 0;
        if (values.Count == 1) return values[0];
        if (values.Count == 2) return (values[0] + values[1]) / 2;

        var mid = values.Count / 2;
        return values.Count % 2 == 0
            ? (values[mid - 1] + values[mid]) / 2
            : values[mid];
    }

    static int CountCrossings(LayoutGraph graph)
    {
        int total = 0;
        for (int r = 0; r < graph.Ranks.Length - 1; r++)
        {
            total += CountCrossingsBetweenRanks(graph, r, r + 1);
        }
        return total;
    }

    static int CountCrossingsBetweenRanks(LayoutGraph graph, int rank1, int rank2)
    {
        // Build list of edges between the two ranks
        var edges = new List<(int sourceOrder, int targetOrder)>();

        foreach (var node in graph.Ranks[rank1])
        {
            foreach (var edge in node.OutEdges)
            {
                var target = graph.GetNode(edge.TargetId);
                if (target is not null && target.Rank == rank2)
                {
                    edges.Add((node.Order, target.Order));
                }
            }
        }

        // Count inversions (crossings) using O(n^2) for simplicity
        // Could be optimized to O(n log n) using merge sort
        int crossings = 0;
        for (int i = 0; i < edges.Count; i++)
        {
            for (int j = i + 1; j < edges.Count; j++)
            {
                var e1 = edges[i];
                var e2 = edges[j];
                if ((e1.sourceOrder < e2.sourceOrder && e1.targetOrder > e2.targetOrder) ||
                    (e1.sourceOrder > e2.sourceOrder && e1.targetOrder < e2.targetOrder))
                {
                    crossings++;
                }
            }
        }

        return crossings;
    }

    static Dictionary<string, int> SaveOrders(LayoutGraph graph)
    {
        return graph.Nodes.Values.ToDictionary(n => n.Id, n => n.Order);
    }

    static void RestoreOrders(LayoutGraph graph, Dictionary<string, int> orders)
    {
        foreach (var (id, order) in orders)
        {
            var node = graph.GetNode(id);
            if (node is not null)
            {
                node.Order = order;
            }
        }
    }
}

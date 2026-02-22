namespace MermaidSharp.Layout;

internal static class Ordering
{
    const int MaxIterations = 24;

    public static void Run(LayoutGraph graph)
    {
        graph.BuildRanks();
        InitializeOrder(graph);

        var bestCrossings = CountCrossings(graph);
        var bestOrders = SaveOrders(graph);

        for (var i = 0; i < MaxIterations && bestCrossings > 0; i++)
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
        for (var r = 0; r < graph.Ranks.Length; r++)
        {
            var nodesInRank = graph.Ranks[r];
            for (var i = 0; i < nodesInRank.Count; i++)
            {
                nodesInRank[i].Order = i;
            }
        }
    }

    static void SweepDown(LayoutGraph graph)
    {
        for (var r = 1; r < graph.Ranks.Length; r++)
        {
            OrderByMedian(graph, r, true);
        }
    }

    static void SweepUp(LayoutGraph graph)
    {
        for (var r = graph.Ranks.Length - 2; r >= 0; r--)
        {
            OrderByMedian(graph, r, false);
        }
    }

    static void OrderByMedian(LayoutGraph graph, int rank, bool useInEdges)
    {
        var nodesInRank = graph.Ranks[rank];
        var positions = new Dictionary<string, double>(nodesInRank.Count);
        var neighborOrders = new List<double>();

        foreach (var node in nodesInRank)
        {
            neighborOrders.Clear();
            foreach (var neighbor in useInEdges
                         ? graph.GetPredecessors(node.Id)
                         : graph.GetSuccessors(node.Id))
            {
                neighborOrders.Add(neighbor.Order);
            }

            if (neighborOrders.Count == 0)
            {
                positions[node.Id] = node.Order;
            }
            else
            {
                neighborOrders.Sort();
                positions[node.Id] = Median(neighborOrders);
            }
        }

        // Sort in-place by median position, maintaining stability for equal positions
        nodesInRank.Sort((a, b) =>
        {
            var cmp = positions[a.Id].CompareTo(positions[b.Id]);
            return cmp != 0 ? cmp : a.Order.CompareTo(b.Order);
        });

        for (var i = 0; i < nodesInRank.Count; i++)
        {
            nodesInRank[i].Order = i;
        }
    }

    static double Median(List<double> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        if (values.Count == 1)
        {
            return values[0];
        }

        if (values.Count == 2)
        {
            return (values[0] + values[1]) / 2;
        }

        var mid = values.Count / 2;
        if (values.Count % 2 == 0)
        {
            return (values[mid - 1] + values[mid]) / 2;
        }

        return values[mid];
    }

    static int CountCrossings(LayoutGraph graph)
    {
        var total = 0;
        for (var r = 0; r < graph.Ranks.Length - 1; r++)
        {
            total += CountCrossingsBetweenRanks(graph, r, r + 1);
        }
        return total;
    }

    static int CountCrossingsBetweenRanks(LayoutGraph graph, int rank1, int rank2)
    {
        // Build list of edges between the two ranks, sorted by source order
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

        if (edges.Count <= 1)
        {
            return 0;
        }

        // Sort by source order (stable), then count inversions in target orders
        // using O(n log n) merge-sort inversion count
        edges.Sort((a, b) =>
        {
            var cmp = a.sourceOrder.CompareTo(b.sourceOrder);
            return cmp != 0 ? cmp : a.targetOrder.CompareTo(b.targetOrder);
        });

        var targetOrders = new int[edges.Count];
        for (var i = 0; i < edges.Count; i++)
        {
            targetOrders[i] = edges[i].targetOrder;
        }

        var buffer = new int[targetOrders.Length];
        return MergeSortCount(targetOrders, buffer, 0, targetOrders.Length - 1);
    }

    static int MergeSortCount(int[] arr, int[] buffer, int left, int right)
    {
        if (left >= right)
        {
            return 0;
        }

        var mid = left + (right - left) / 2;
        var count = MergeSortCount(arr, buffer, left, mid)
                  + MergeSortCount(arr, buffer, mid + 1, right);

        // Merge and count inversions
        var i = left;
        var j = mid + 1;
        var k = left;

        while (i <= mid && j <= right)
        {
            if (arr[i] <= arr[j])
            {
                buffer[k++] = arr[i++];
            }
            else
            {
                // All remaining elements in left half form inversions with arr[j]
                count += mid - i + 1;
                buffer[k++] = arr[j++];
            }
        }

        while (i <= mid)
        {
            buffer[k++] = arr[i++];
        }

        while (j <= right)
        {
            buffer[k++] = arr[j++];
        }

        Array.Copy(buffer, left, arr, left, right - left + 1);
        return count;
    }

    static Dictionary<string, int> SaveOrders(LayoutGraph graph) =>
        graph.Nodes.Values.ToDictionary(_ => _.Id, _ => _.Order);

    static void RestoreOrders(LayoutGraph graph, Dictionary<string, int> orders)
    {
        foreach (var (id, order) in orders)
        {
            var node = graph.GetNode(id);
            node?.Order = order;
        }
    }
}

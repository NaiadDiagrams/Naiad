namespace Naiad.Layout;

static class Ranker
{
    public static void Run(LayoutGraph graph, RankerType rankerType)
    {
        switch (rankerType)
        {
            case RankerType.LongestPath:
                LongestPath(graph);
                break;
            case RankerType.TightTree:
                TightTree(graph);
                break;
            case RankerType.NetworkSimplex:
                NetworkSimplex(graph);
                break;
        }

        NormalizeRanks(graph);
        InsertDummyNodes(graph);
    }

    static void LongestPath(LayoutGraph graph)
    {
        var visited = new HashSet<string>();

        foreach (var node in graph.Nodes.Values)
        {
            if (!visited.Contains(node.Id))
            {
                DfsLongestPath(graph, node, visited);
            }
        }
    }

    static int DfsLongestPath(LayoutGraph graph, LayoutNode node, HashSet<string> visited)
    {
        if (!visited.Add(node.Id))
        {
            return node.Rank;
        }

        var maxPredRank = -1;
        foreach (var pred in graph.GetPredecessors(node.Id))
        {
            var predRank = DfsLongestPath(graph, pred, visited);
            maxPredRank = Math.Max(maxPredRank, predRank);
        }

        node.Rank = maxPredRank + 1;
        return node.Rank;
    }

    static readonly Comparison<LayoutNode> rankDescending = (a, b) => b.Rank.CompareTo(a.Rank);

    static void TightTree(LayoutGraph graph)
    {
        // Tight tree is similar to longest path but considers edge weights
        // For simplicity, we'll use longest path with slight optimization
        LongestPath(graph);

        var nodes = new List<LayoutNode>(graph.Nodes.Values);

        // Pull nodes down to minimize edge length where possible
        bool changed;
        do
        {
            changed = false;
            nodes.Sort(rankDescending);
            foreach (var node in nodes)
            {
                var minSuccRank = int.MaxValue;
                foreach (var edge in node.OutEdges)
                {
                    if (edge.Target is { } succ && succ.Rank < minSuccRank)
                    {
                        minSuccRank = succ.Rank;
                    }
                }

                if (minSuccRank == int.MaxValue)
                {
                    continue;
                }

                var targetRank = minSuccRank - 1;
                if (targetRank <= node.Rank)
                {
                    continue;
                }

                var maxPredRank = -1;
                foreach (var edge in node.InEdges)
                {
                    if (edge.Source is { } pred && pred.Rank > maxPredRank)
                    {
                        maxPredRank = pred.Rank;
                    }
                }

                var minAllowedRank = maxPredRank == -1 ? 0 : maxPredRank + 1;

                if (targetRank >= minAllowedRank)
                {
                    node.Rank = targetRank;
                    changed = true;
                }
            }
        } while (changed);
    }

    static void NetworkSimplex(LayoutGraph graph) =>
        // Network simplex is complex - fall back to tight tree for now
        // Full implementation would use linear programming approach
        TightTree(graph);

    static void NormalizeRanks(LayoutGraph graph)
    {
        if (graph.Nodes.Count == 0)
        {
            return;
        }

        var minRank = graph.Nodes.Values.Min(_ => _.Rank);
        foreach (var node in graph.Nodes.Values)
        {
            node.Rank -= minRank;
        }
    }

    static void InsertDummyNodes(LayoutGraph graph)
    {
        var edgesToProcess = graph.Edges.ToList();
        var dummyCount = 0;

        foreach (var edge in edgesToProcess)
        {
            var source = graph.GetNode(edge.SourceId);
            var target = graph.GetNode(edge.TargetId);
            if (source is null || target is null)
            {
                continue;
            }

            var rankDiff = target.Rank - source.Rank;
            if (rankDiff > 1)
            {
                // Need dummy nodes
                var prevNodeId = edge.SourceId;
                for (var r = source.Rank + 1; r < target.Rank; r++)
                {
                    var dummyId = $"_dummy_{dummyCount++}";
                    var dummy = new LayoutNode
                    {
                        Id = dummyId,
                        Width = 0,
                        Height = 0,
                        Rank = r,
                        IsDummy = true,
                        OriginalEdgeSource = edge.SourceId,
                        OriginalEdgeTarget = edge.TargetId
                    };
                    graph.AddNode(dummy);

                    var newEdge = new LayoutEdge
                    {
                        SourceId = prevNodeId,
                        TargetId = dummyId
                    };
                    graph.AddEdge(newEdge);

                    prevNodeId = dummyId;
                }

                // Connect last dummy to target
                var finalEdge = new LayoutEdge
                {
                    SourceId = prevNodeId,
                    TargetId = edge.TargetId
                };
                graph.AddEdge(finalEdge);

                // Remove original edge connections
                source.OutEdges.Remove(edge);
                target.InEdges.Remove(edge);
            }
        }
    }
}

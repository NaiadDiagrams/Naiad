static class Ranker
{
    public static void Run(LayoutGraph graph, RankerType rankerType)
    {
        AssignRanks(graph, rankerType);
        InsertDummyNodes(graph);
    }

    /// <summary>Assigns and normalizes ranks without splitting long edges, so callers can adjust ranks
    /// (e.g. cluster confinement) before <see cref="InsertDummyNodes"/> runs.</summary>
    public static void AssignRanks(LayoutGraph graph, RankerType rankerType)
    {
        if (graph.Edges.Any(_ => _.IsSameRank))
        {
            // Same-rank constraints need group-aware ranking; the plain rankers
            // assume every edge increases the rank.
            RankSameRankGroups(graph);
        }
        else
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
        }

        NormalizeRanks(graph);
    }

    /// <summary>
    /// Ranks a graph that contains same-rank constraints by condensing each set
    /// of same-rank nodes into one group and running longest-path on the groups.
    /// </summary>
    static void RankSameRankGroups(LayoutGraph graph)
    {
        // Union-find over nodes joined by same-rank edges.
        var parent = new Dictionary<string, string>();
        foreach (var node in graph.Nodes.Values)
        {
            parent[node.Id] = node.Id;
        }

        string Find(string x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        foreach (var edge in graph.Edges)
        {
            if (edge.IsSameRank &&
                parent.ContainsKey(edge.SourceId) &&
                parent.ContainsKey(edge.TargetId))
            {
                parent[Find(edge.SourceId)] = Find(edge.TargetId);
            }
        }

        // Predecessor groups from ordinary edges (using the acyclic direction).
        var groupPreds = new Dictionary<string, HashSet<string>>();
        foreach (var node in graph.Nodes.Values)
        {
            groupPreds.TryAdd(Find(node.Id), []);
        }

        foreach (var edge in graph.Edges)
        {
            if (edge.IsSameRank)
            {
                continue;
            }

            var sourceId = edge.IsReversed ? edge.TargetId : edge.SourceId;
            var targetId = edge.IsReversed ? edge.SourceId : edge.TargetId;
            if (!parent.ContainsKey(sourceId) ||
                !parent.ContainsKey(targetId))
            {
                continue;
            }

            var sourceGroup = Find(sourceId);
            var targetGroup = Find(targetId);
            if (sourceGroup != targetGroup)
            {
                groupPreds[targetGroup].Add(sourceGroup);
            }
        }

        // Longest-path rank over groups (with a cycle guard).
        var groupRank = new Dictionary<string, int>();
        var visiting = new HashSet<string>();

        int RankGroup(string group)
        {
            if (groupRank.TryGetValue(group, out var rank))
            {
                return rank;
            }

            if (!visiting.Add(group))
            {
                return 0;
            }

            var maxPred = -1;
            foreach (var pred in groupPreds[group])
            {
                maxPred = Math.Max(maxPred, RankGroup(pred));
            }

            visiting.Remove(group);
            groupRank[group] = maxPred + 1;
            return maxPred + 1;
        }

        foreach (var group in groupPreds.Keys)
        {
            RankGroup(group);
        }

        foreach (var node in graph.Nodes.Values)
        {
            node.Rank = groupRank[Find(node.Id)];
        }
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

    public static void InsertDummyNodes(LayoutGraph graph)
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
                // The label rides the middle waypoint; sizing that dummy to the
                // label reserves space so neighbouring nodes are kept clear of it.
                var labelRank = source.Rank + rankDiff / 2;

                // Need dummy nodes
                var prevNodeId = edge.SourceId;
                for (var r = source.Rank + 1; r < target.Rank; r++)
                {
                    var dummyId = $"_dummy_{dummyCount++}";
                    var carriesLabel = edge.LabelWidth > 0 && r == labelRank;
                    var dummy = new LayoutNode
                    {
                        Id = dummyId,
                        Width = carriesLabel ? edge.LabelWidth : 0,
                        Height = carriesLabel ? edge.LabelHeight : 0,
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

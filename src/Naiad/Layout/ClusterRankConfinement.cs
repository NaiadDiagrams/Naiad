/// <summary>
/// Pulls a cluster's "slack" members (typically sinks, e.g. error nodes or detached observability sinks)
/// down toward the cluster's lowest rank, so every subgraph occupies a tight contiguous rank band instead
/// of being torn across the diagram by members whose feeding edges arrive at very different ranks. Runs
/// after ranking but before long edges are split into dummy nodes. Only ever increases a node's rank, and
/// never past its successors, so the ranking stays acyclic and feasible.
/// </summary>
static class ClusterRankConfinement
{
    public static void Run(LayoutGraph graph, GraphDiagramBase diagram)
    {
        if (diagram.Subgraphs.Count == 0)
        {
            return;
        }

        var innermost = new Dictionary<string, string>(StringComparer.Ordinal);
        var depth = new Dictionary<string, int>(StringComparer.Ordinal);

        void Walk(Subgraph subgraph, int d)
        {
            depth[subgraph.Id] = d;
            foreach (var nodeId in subgraph.NodeIds)
            {
                innermost[nodeId] = subgraph.Id;
            }

            foreach (var nested in subgraph.NestedSubgraphs)
            {
                Walk(nested, d + 1);
            }
        }

        foreach (var subgraph in diagram.Subgraphs)
        {
            Walk(subgraph, 0);
        }

        var members = new Dictionary<string, List<LayoutNode>>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes.Values)
        {
            if (node.IsDummy)
            {
                continue;
            }

            if (innermost.TryGetValue(node.Id, out var clusterId))
            {
                if (!members.TryGetValue(clusterId, out var list))
                {
                    list = [];
                    members[clusterId] = list;
                }

                list.Add(node);
            }
        }

        // Innermost clusters first so a nested cluster is compacted before its parent.
        foreach (var entry in members.OrderByDescending(_ => depth[_.Key]))
        {
            var list = entry.Value;
            if (list.Count < 2)
            {
                continue;
            }

            var clusterBottom = list.Max(_ => _.Rank);
            foreach (var node in list)
            {
                var minSuccessor = int.MaxValue;
                foreach (var edge in node.OutEdges)
                {
                    if (edge.Target is { } target)
                    {
                        minSuccessor = Math.Min(minSuccessor, target.Rank);
                    }
                }

                var ceiling = minSuccessor == int.MaxValue
                    ? clusterBottom
                    : Math.Min(clusterBottom, minSuccessor - 1);

                if (ceiling > node.Rank)
                {
                    node.Rank = ceiling;
                }
            }
        }

        CompactRanks(graph);
    }

    // Removes any rank left empty by the push-down so ranks stay consecutive.
    static void CompactRanks(LayoutGraph graph)
    {
        var used = new SortedSet<int>();
        foreach (var node in graph.Nodes.Values)
        {
            used.Add(node.Rank);
        }

        var map = new Dictionary<int, int>();
        var next = 0;
        foreach (var rank in used)
        {
            map[rank] = next++;
        }

        foreach (var node in graph.Nodes.Values)
        {
            node.Rank = map[node.Rank];
        }
    }
}

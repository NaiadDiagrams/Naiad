/// <summary>
/// Adds left/right border "walls" to every subgraph and reorders each rank so the subgraph's nodes (and
/// any nested subgraphs) stay contiguous between its walls. The walls are thin nodes chained vertically,
/// so coordinate assignment aligns them into straight lines: this keeps each cluster a compact rectangle
/// (low shear) and reserves a horizontal lane foreign nodes can't enter, so cluster boxes don't overlap.
/// Dummy edge nodes inherit the common-ancestor cluster of the edge they split, so a long edge that stays
/// inside one cluster routes inside its box.
/// </summary>
static class ClusterOrdering
{
    public static void Run(LayoutGraph graph, GraphDiagramBase diagram, double borderWidth)
    {
        if (diagram.Subgraphs.Count == 0)
        {
            return;
        }

        var chains = BuildNodeChains(diagram);

        // Dummy nodes take the common cluster prefix of the edge they split.
        foreach (var node in graph.Nodes.Values)
        {
            if (node is { IsDummy: true, OriginalEdgeSource: { } source, OriginalEdgeTarget: { } target })
            {
                chains[node.Id] = CommonPrefix(
                    chains.GetValueOrDefault(source, []),
                    chains.GetValueOrDefault(target, []));
            }
        }

        AddBorders(graph, diagram, chains, borderWidth);

        // Tag every node with its top-level cluster so coordinate assignment can keep clusters cohesive.
        foreach (var node in graph.Nodes.Values)
        {
            if (chains.TryGetValue(node.Id, out var chain) && chain.Count > 0)
            {
                node.ClusterId = chain[0];
            }
        }

        graph.BuildRanks();

        // A consistent left-to-right key per cluster (its members' average normalized order) so the cluster
        // keeps the same lane on every rank, instead of flipping sides and shearing wide.
        var laneKey = ComputeLaneKeys(graph, chains);

        foreach (var rank in graph.Ranks)
        {
            rank.Sort((a, b) => a.Order.CompareTo(b.Order));
            var arranged = Arrange(rank, chains, 0, laneKey);
            for (var i = 0; i < arranged.Count; i++)
            {
                arranged[i].Order = i;
            }
        }

        graph.UpdateOrderInRanks();
    }

    // Average normalized order (0..1) of each node and, for each cluster, of its real members. Used as a
    // rank-independent horizontal key so clusters occupy stable lanes across ranks.
    static Dictionary<string, double> ComputeLaneKeys(LayoutGraph graph, Dictionary<string, List<string>> chains)
    {
        var keys = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var rank in graph.Ranks)
        {
            var n = rank.Count;
            foreach (var node in rank)
            {
                keys[node.Id] = n <= 1 ? 0.5 : node.Order / (double) (n - 1);
            }
        }

        var sum = new Dictionary<string, double>(StringComparer.Ordinal);
        var count = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes.Values)
        {
            if (node.IsDummy || node.IsClusterBorder || !chains.TryGetValue(node.Id, out var chain))
            {
                continue;
            }

            foreach (var clusterId in chain)
            {
                sum[clusterId] = sum.GetValueOrDefault(clusterId) + keys.GetValueOrDefault(node.Id);
                count[clusterId] = count.GetValueOrDefault(clusterId) + 1;
            }
        }

        foreach (var (clusterId, total) in sum)
        {
            keys[clusterId] = total / count[clusterId];
        }

        return keys;
    }

    static void AddBorders(
        LayoutGraph graph,
        GraphDiagramBase diagram,
        Dictionary<string, List<string>> chains,
        double borderWidth)
    {
        var subgraphChains = BuildSubgraphChains(diagram);

        // Gather each subgraph's real members (a node belongs to its innermost subgraph and all ancestors).
        var membersByCluster = new Dictionary<string, List<LayoutNode>>(StringComparer.Ordinal);
        foreach (var node in graph.Nodes.Values)
        {
            if (node.IsDummy || node.IsClusterBorder || !chains.TryGetValue(node.Id, out var chain))
            {
                continue;
            }

            foreach (var clusterId in chain)
            {
                if (!membersByCluster.TryGetValue(clusterId, out var list))
                {
                    list = [];
                    membersByCluster[clusterId] = list;
                }

                list.Add(node);
            }
        }

        var borderId = 0;
        foreach (var subgraph in Flatten(diagram.Subgraphs))
        {
            if (!membersByCluster.TryGetValue(subgraph.Id, out var members) ||
                members.Count == 0)
            {
                continue;
            }

            var minRank = members.Min(_ => _.Rank);
            var maxRank = members.Max(_ => _.Rank);

            // A representative column so the walls start near their cluster in the initial order.
            var orders = members.Select(_ => _.Order).Order().ToList();
            var columnOrder = orders[orders.Count / 2];
            var clusterChain = subgraphChains[subgraph.Id];

            LayoutNode? previousLeft = null;
            LayoutNode? previousRight = null;
            for (var rank = minRank; rank <= maxRank; rank++)
            {
                var left = MakeBorder(borderId++, rank, columnOrder, borderWidth, isLeft: true);
                var right = MakeBorder(borderId++, rank, columnOrder, borderWidth, isLeft: false);
                chains[left.Id] = clusterChain;
                chains[right.Id] = clusterChain;
                graph.AddNode(left);
                graph.AddNode(right);

                // Chain the walls vertically so coordinate assignment keeps them straight.
                if (previousLeft is not null)
                {
                    graph.AddEdge(new() { SourceId = previousLeft.Id, TargetId = left.Id });
                }

                if (previousRight is not null)
                {
                    graph.AddEdge(new() { SourceId = previousRight.Id, TargetId = right.Id });
                }

                previousLeft = left;
                previousRight = right;
            }
        }
    }

    static LayoutNode MakeBorder(int id, int rank, int order, double width, bool isLeft) =>
        new()
        {
            Id = $"_border_{id}",
            Width = width,
            Height = 0,
            Rank = rank,
            Order = order,
            IsClusterBorder = true,
            IsLeftBorder = isLeft
        };

    // Recursively groups a rank's nodes by cluster at the given depth; a cluster's own walls are pulled to
    // its left and right edges, and clusters/free nodes between them are ordered by their stable lane key so
    // a cluster keeps the same side on every rank.
    static List<LayoutNode> Arrange(
        List<LayoutNode> nodes,
        Dictionary<string, List<string>> chains,
        int depth,
        Dictionary<string, double> laneKey)
    {
        var leftBorders = new List<LayoutNode>();
        var rightBorders = new List<LayoutNode>();
        var clusters = new Dictionary<string, List<LayoutNode>>(StringComparer.Ordinal);
        var sequence = new List<object>();

        foreach (var node in nodes)
        {
            var chain = chains.GetValueOrDefault(node.Id);
            var key = chain is not null && depth < chain.Count ? chain[depth] : null;

            if (key is null)
            {
                if (node.IsClusterBorder)
                {
                    (node.IsLeftBorder ? leftBorders : rightBorders).Add(node);
                }
                else
                {
                    sequence.Add(node);
                }

                continue;
            }

            if (!clusters.TryGetValue(key, out var members))
            {
                members = [];
                clusters[key] = members;
                sequence.Add(key);
            }

            members.Add(node);
        }

        double KeyOf(object item) =>
            item is LayoutNode node
                ? laneKey.GetValueOrDefault(node.Id, 0.5)
                : laneKey.GetValueOrDefault((string) item, 0.5);

        // Stable sort by lane key keeps clusters in a consistent left-to-right order across ranks.
        var ordered = sequence
            .Select((item, index) => (item, index))
            .OrderBy(_ => KeyOf(_.item))
            .ThenBy(_ => _.index)
            .Select(_ => _.item);

        var result = new List<LayoutNode>(nodes.Count);
        result.AddRange(leftBorders);
        foreach (var item in ordered)
        {
            if (item is LayoutNode node)
            {
                result.Add(node);
            }
            else
            {
                result.AddRange(Arrange(clusters[(string) item], chains, depth + 1, laneKey));
            }
        }

        result.AddRange(rightBorders);
        return result;
    }

    // Maps each real node id to its subgraph ancestor chain (outermost first).
    static Dictionary<string, List<string>> BuildNodeChains(GraphDiagramBase diagram)
    {
        var chains = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Walk(Subgraph subgraph, List<string> ancestors)
        {
            var chain = new List<string>(ancestors) { subgraph.Id };
            foreach (var nodeId in subgraph.NodeIds)
            {
                chains[nodeId] = chain;
            }

            foreach (var nested in subgraph.NestedSubgraphs)
            {
                Walk(nested, chain);
            }
        }

        foreach (var subgraph in diagram.Subgraphs)
        {
            Walk(subgraph, []);
        }

        return chains;
    }

    // Maps each subgraph id to its own ancestor chain (outermost first, ending with itself).
    static Dictionary<string, List<string>> BuildSubgraphChains(GraphDiagramBase diagram)
    {
        var chains = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        void Walk(Subgraph subgraph, List<string> ancestors)
        {
            var chain = new List<string>(ancestors) { subgraph.Id };
            chains[subgraph.Id] = chain;
            foreach (var nested in subgraph.NestedSubgraphs)
            {
                Walk(nested, chain);
            }
        }

        foreach (var subgraph in diagram.Subgraphs)
        {
            Walk(subgraph, []);
        }

        return chains;
    }

    static IEnumerable<Subgraph> Flatten(IEnumerable<Subgraph> subgraphs)
    {
        foreach (var subgraph in subgraphs)
        {
            yield return subgraph;
            foreach (var nested in Flatten(subgraph.NestedSubgraphs))
            {
                yield return nested;
            }
        }
    }

    static List<string> CommonPrefix(List<string> a, List<string> b)
    {
        var prefix = new List<string>();
        var count = Math.Min(a.Count, b.Count);
        for (var i = 0; i < count && a[i] == b[i]; i++)
        {
            prefix.Add(a[i]);
        }

        return prefix;
    }
}

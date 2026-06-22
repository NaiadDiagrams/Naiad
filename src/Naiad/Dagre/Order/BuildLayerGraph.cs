namespace Naiad.Dagre;

static class BuildLayerGraph
{
    public static Graph Run(Graph graph, int rank, string relationship, List<string>? nodesWithRank = null)
    {
        nodesWithRank ??= graph.Nodes();

        var root = CreateRootNode(graph);
        var result = new Graph(compound: true)
            .SetGraph(
                new()
                {
                    Root = root
                })
            .SetDefaultNodeLabel(graph.NodeLabel);

        foreach (var v in nodesWithRank)
        {
            var node = graph.NodeLabel(v);
            var parent = graph.Parent(v);

            if (node.Rank == rank ||
                (node is {MinRank: not null, MaxRank: not null} &&
                 node.MinRank <= rank && rank <= node.MaxRank))
            {
                result.SetNode(v);
                result.SetParent(v, parent ?? root);

                // This assumes we have only short edges!
                var edges = relationship == "inEdges" ? graph.InEdges(v) : graph.OutEdges(v);
                if (edges != null)
                {
                    foreach (var e in edges)
                    {
                        var u = e.V == v ? e.W : e.V;
                        var weight = result.TryGetEdgeLabel(u, v, out var existing) ? existing.Weight!.Value : 0;
                        result.SetEdge(u, v, new() { Weight = graph.FindEdgeLabel(e).Weight!.Value + weight });
                    }
                }

                if (node.MinRank != null)
                {
                    result.SetNode(v, new()
                    {
                        BorderLeftId = node.BorderLeft![rank],
                        BorderRightId = node.BorderRight![rank]
                    });
                }
            }
        }

        return result;
    }

    static string CreateRootNode(Graph graph)
    {
        string v;
        while (graph.HasNode(v = graph.UniqueId("_root")))
        {
        }

        return v;
    }
}

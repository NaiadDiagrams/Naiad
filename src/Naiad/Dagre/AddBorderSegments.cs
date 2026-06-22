namespace Naiad.Dagre;

static class AddBorderSegments
{
    public static void Run(Graph graph)
    {
        void Dfs(string v)
        {
            var children = graph.Children(v);
            var node = graph.NodeLabel(v);
            if (children.Count != 0)
            {
                foreach (var child in children)
                {
                    Dfs(child);
                }
            }

            if (node.MinRank != null)
            {
                node.BorderLeft = [];
                node.BorderRight = [];
                for (int rank = node.MinRank!.Value, maxRank = node.MaxRank!.Value + 1;
                    rank < maxRank;
                    ++rank)
                {
                    AddBorderNode(graph, BorderKind.Left, "_bl", v, node, rank);
                    AddBorderNode(graph, BorderKind.Right, "_br", v, node, rank);
                }
            }
        }

        foreach (var v in graph.Children(Util.GraphNode))
        {
            Dfs(v);
        }
    }

    static void AddBorderNode(
        Graph graph,
        BorderKind prop,
        string prefix,
        string sg,
        NodeLabel sgNode,
        int rank)
    {
        var label = new NodeLabel
        {
            Width = 0,
            Height = 0,
            Rank = rank,
            BorderType = prop
        };
        var list = prop == BorderKind.Left ? sgNode.BorderLeft! : sgNode.BorderRight!;
        var prev = rank - 1 >= 0 && rank - 1 < list.Count ? list[rank - 1] : null;
        var curr = Util.AddDummyNode(graph, DummyKind.Border, label, prefix);
        while (list.Count <= rank)
        {
            list.Add(null!);
        }

        list[rank] = curr;
        graph.SetParent(curr, sg);
        if (prev != null)
        {
            graph.SetEdge(
                prev,
                curr,
                new()
                {
                    Weight = 1
                });
        }
    }
}

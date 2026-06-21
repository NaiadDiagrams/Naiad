namespace Naiad.Dagre;

static class AddBorderSegments
{
    public static void Run(Graph graph)
    {
        void Dfs(string v)
        {
            var children = graph.Children(v);
            var node = graph.Node(v);
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
                    AddBorderNode(graph, "borderLeft", "_bl", v, node, rank);
                    AddBorderNode(graph, "borderRight", "_br", v, node, rank);
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
        string prop,
        string prefix,
        string sg,
        NodeLabel sgNode,
        int rank)
    {
        var label = new NodeLabel { Width = 0, Height = 0, Rank = rank, BorderType = prop };
        var list = prop == "borderLeft" ? sgNode.BorderLeft! : sgNode.BorderRight!;
        var prev = rank - 1 >= 0 && rank - 1 < list.Count ? list[rank - 1] : null;
        var curr = Util.AddDummyNode(graph, "border", label, prefix);
        while (list.Count <= rank)
        {
            list.Add(null!);
        }

        list[rank] = curr;
        graph.SetParent(curr, sg);
        if (prev != null)
        {
            graph.SetEdge(prev, curr, new EdgeLabel { Weight = 1 });
        }
    }
}

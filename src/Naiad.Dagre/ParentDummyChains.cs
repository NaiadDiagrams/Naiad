namespace Naiad.Dagre;

static class ParentDummyChains
{
    sealed class PostorderNum
    {
        public int Low;
        public int Lim;
    }

    sealed class PathData
    {
        public List<string?> Path = [];
        public string? Lca;
    }

    public static void Run(Graph graph)
    {
        var postorderNums = Postorder(graph);

        foreach (var v0 in graph.Graph_().DummyChains!)
        {
            var v = v0;
            var node = graph.Node(v);
            var edgeObj = node.EdgeObj!;
            var pathData = FindPath(graph, postorderNums, edgeObj.V, edgeObj.W);
            var path = pathData.Path;
            var lca = pathData.Lca;
            var pathIdx = 0;
            var pathV = path[pathIdx];
            var ascending = true;

            while (v != edgeObj.W)
            {
                node = graph.Node(v);

                if (ascending)
                {
                    while ((pathV = path[pathIdx]) != lca &&
                        graph.Node(pathV!).MaxRank!.Value < node.Rank!.Value)
                    {
                        pathIdx++;
                    }

                    if (pathV == lca)
                    {
                        ascending = false;
                    }
                }

                if (!ascending)
                {
                    while (pathIdx < path.Count - 1 &&
                        graph.Node(path[pathIdx + 1]!).MinRank!.Value <= node.Rank!.Value)
                    {
                        pathIdx++;
                    }

                    pathV = path[pathIdx];
                }

                if (pathV != null)
                {
                    graph.SetParent(v, pathV);
                }

                v = graph.Successors(v)![0]!;
            }
        }
    }

    // Find a path from v to w through the lowest common ancestor (LCA). Return the
    // full path and the LCA.
    static PathData FindPath(
        Graph graph,
        Dictionary<string, PostorderNum> postorderNums,
        string v,
        string w)
    {
        var vPath = new List<string?>();
        var wPath = new List<string?>();
        var low = Math.Min(postorderNums[v]!.Low, postorderNums[w]!.Low);
        var lim = Math.Max(postorderNums[v]!.Lim, postorderNums[w]!.Lim);
        string? parent;

        // Traverse up from v to find the LCA
        parent = v;
        do
        {
            parent = graph.Parent(parent!);
            vPath.Add(parent);
        } while (parent != null &&
            (postorderNums[parent]!.Low > low || lim > postorderNums[parent]!.Lim));
        var lca = parent;

        // Traverse from w to LCA
        var wParent = w;
        while ((wParent = graph.Parent(wParent)!) != lca)
        {
            wPath.Add(wParent);
        }

        wPath.Reverse();
        var path = new List<string?>(vPath);
        path.AddRange(wPath);
        return new PathData { Path = path, Lca = lca };
    }

    static Dictionary<string, PostorderNum> Postorder(Graph graph)
    {
        var result = new Dictionary<string, PostorderNum>(StringComparer.Ordinal);
        var lim = 0;

        void Dfs(string v)
        {
            var low = lim;
            foreach (var child in graph.Children(v))
            {
                Dfs(child);
            }

            result[v] = new PostorderNum { Low = low, Lim = lim++ };
        }

        foreach (var v in graph.Children(Util.GraphNode))
        {
            Dfs(v);
        }

        return result;
    }
}

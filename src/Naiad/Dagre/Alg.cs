namespace Naiad.Dagre;

/// <summary>Depth-first traversal (pre/post-order) used by the network-simplex ranking pass.</summary>
static class Alg
{
    static T Reduce<T>(Graph graph, IReadOnlyList<string> vs, bool postorder, Func<T, string, T> fn, T acc)
    {
        List<string> Navigation(string v) =>
            (graph.IsDirected ? graph.Successors(v) : graph.Neighbors(v)) ?? [];

        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var v in vs)
        {
            if (!graph.HasNode(v))
            {
                throw new InvalidOperationException("Graph does not have node: " + v);
            }

            acc = DoReduce(v, postorder, visited, Navigation, fn, acc);
        }

        return acc;
    }

    static T DoReduce<T>(string v, bool postorder, HashSet<string> visited, Func<string, List<string>> navigation, Func<T, string, T> fn, T acc)
    {
        if (visited.Add(v))
        {
            if (!postorder)
            {
                acc = fn(acc, v);
            }

            foreach (var w in navigation(v))
            {
                acc = DoReduce(w, postorder, visited, navigation, fn, acc);
            }

            if (postorder)
            {
                acc = fn(acc, v);
            }
        }

        return acc;
    }

    static List<string> Dfs(Graph graph, IReadOnlyList<string> vs, bool postorder)
    {
        var acc = new List<string>();
        Reduce(graph, vs, postorder, (a, v) =>
        {
            a.Add(v);
            return a;
        }, acc);
        return acc;
    }

    public static List<string> Preorder(Graph graph, IReadOnlyList<string> vs) => Dfs(graph, vs, false);

    public static List<string> Postorder(Graph graph, IReadOnlyList<string> vs) => Dfs(graph, vs, true);
}

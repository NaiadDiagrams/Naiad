/// <summary>
/// Makes a graph acyclic by reversing edges that participate in cycles, then restores them later.
/// </summary>
static class Acyclic
{
    public static void Run(Graph graph)
    {
        foreach (var e in DfsFas(graph))
        {
            var label = graph.FindEdgeLabel(e);
            graph.RemoveEdge(e);
            label.ForwardName = e.Name;
            label.Reversed = true;
            graph.SetEdge(e.W, e.V, label, graph.UniqueId("rev"));
        }
    }

    static List<EdgeKey> DfsFas(Graph graph)
    {
        var fas = new List<EdgeKey>();
        var stack = new Dictionary<string, bool>(StringComparer.Ordinal);
        var visited = new Dictionary<string, bool>(StringComparer.Ordinal);

        void Dfs(string v)
        {
            if (!visited.TryAdd(v, true))
            {
                return;
            }

            stack[v] = true;
            foreach (var e in graph.OutEdgesOf(v))
            {
                if (stack.ContainsKey(e.W))
                {
                    fas.Add(e);
                }
                else
                {
                    Dfs(e.W);
                }
            }

            stack.Remove(v);
        }

        foreach (var v in graph.Nodes())
        {
            Dfs(v);
        }

        return fas;
    }

    public static void Undo(Graph graph)
    {
        foreach (var e in graph.Edges())
        {
            var label = graph.FindEdgeLabel(e);
            if (label.Reversed == true)
            {
                graph.RemoveEdge(e);

                var forwardName = label.ForwardName;
                label.Reversed = null;
                label.ForwardName = null;
                graph.SetEdge(e.W, e.V, label, forwardName);
            }
        }
    }
}

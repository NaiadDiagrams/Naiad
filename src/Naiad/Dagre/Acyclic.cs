namespace Naiad.Dagre;

/// <summary>
/// Makes a graph acyclic by reversing edges that participate in cycles, then restores them later.
/// </summary>
static class Acyclic
{
    public static void Run(Graph graph)
    {
        var fas = graph.Graph_().Acyclicer == "greedy"
            ? GreedyFas.Run(graph, WeightFn(graph))
            : DfsFas(graph);

        foreach (var e in fas)
        {
            var label = graph.Edge_(e);
            graph.RemoveEdge(e);
            label.ForwardName = e.Name;
            label.Reversed = true;
            graph.SetEdge(e.W, e.V, label, graph.UniqueId("rev"));
        }

        return;

        static Func<Edge, double> WeightFn(Graph g) =>
            e => g.Edge_(e).Weight!.Value;
    }

    static List<Edge> DfsFas(Graph graph)
    {
        var fas = new List<Edge>();
        var stack = new Dictionary<string, bool>(StringComparer.Ordinal);
        var visited = new Dictionary<string, bool>(StringComparer.Ordinal);

        void Dfs(string v)
        {
            if (!visited.TryAdd(v, true))
            {
                return;
            }

            stack[v] = true;
            foreach (var e in graph.OutEdges(v)!)
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
            var label = graph.Edge_(e);
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

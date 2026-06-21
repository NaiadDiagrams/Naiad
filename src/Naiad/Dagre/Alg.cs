namespace Naiad.Dagre;

/// <summary>Graph algorithms (traversal, connected components, cycle detection) used by the layout passes.</summary>
static class Alg
{
    static T Reduce<T>(Graph g, IReadOnlyList<string> vs, bool postorder, Func<T, string, T> fn, T acc)
    {
        List<string> Navigation(string v) =>
            (g.IsDirected() ? g.Successors(v) : g.Neighbors(v)) ?? [];

        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var v in vs)
        {
            if (!g.HasNode(v))
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

    static List<string> Dfs(Graph g, IReadOnlyList<string> vs, bool postorder)
    {
        var acc = new List<string>();
        Reduce(g, vs, postorder, (a, v) =>
        {
            a.Add(v);
            return a;
        }, acc);
        return acc;
    }

    public static List<string> Preorder(Graph g, IReadOnlyList<string> vs) => Dfs(g, vs, false);

    public static List<string> Postorder(Graph g, IReadOnlyList<string> vs) => Dfs(g, vs, true);

    public static List<List<string>> Components(Graph graph)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var cmpts = new List<List<string>>();
        List<string> cmpt;

        void Dfs(string v)
        {
            if (!visited.Add(v))
            {
                return;
            }

            cmpt.Add(v);
            foreach (var w in graph.Successors(v)!)
            {
                Dfs(w);
            }

            foreach (var w in graph.Predecessors(v)!)
            {
                Dfs(w);
            }
        }

        foreach (var v in graph.Nodes())
        {
            cmpt = [];
            Dfs(v);
            if (cmpt.Count > 0)
            {
                cmpts.Add(cmpt);
            }
        }

        return cmpts;
    }

    sealed class VisitedEntry
    {
        public bool OnStack;
        public int Lowlink;
        public int Index;
    }

    public static List<List<string>> Tarjan(Graph graph)
    {
        var index = 0;
        var stack = new List<string>();
        var visited = new Dictionary<string, VisitedEntry>(StringComparer.Ordinal);
        var results = new List<List<string>>();

        void Dfs(string v)
        {
            var entry = new VisitedEntry { OnStack = true, Lowlink = index, Index = index };
            index++;
            visited[v] = entry;
            stack.Add(v);

            foreach (var w in graph.Successors(v)!)
            {
                if (!visited.TryGetValue(w, out var wEntry))
                {
                    Dfs(w);
                    entry.Lowlink = Math.Min(entry.Lowlink, visited[w].Lowlink);
                }
                else if (wEntry.OnStack)
                {
                    entry.Lowlink = Math.Min(entry.Lowlink, wEntry.Index);
                }
            }

            if (entry.Lowlink == entry.Index)
            {
                var cmpt = new List<string>();
                string w;
                do
                {
                    w = stack[^1];
                    stack.RemoveAt(stack.Count - 1);
                    visited[w].OnStack = false;
                    cmpt.Add(w);
                }
                while (v != w);

                results.Add(cmpt);
            }
        }

        foreach (var v in graph.Nodes())
        {
            if (!visited.ContainsKey(v))
            {
                Dfs(v);
            }
        }

        return results;
    }

    public static List<List<string>> FindCycles(Graph graph) =>
        Tarjan(graph).Where(cmpt => cmpt.Count > 1 || (cmpt.Count == 1 && graph.HasEdge(cmpt[0], cmpt[0]))).ToList();
}

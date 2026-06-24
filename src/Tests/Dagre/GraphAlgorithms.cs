/// <summary>
/// Connected-components and cycle-detection algorithms used only as assertion oracles by the ported dagre
/// tests (e.g. verifying that <c>Acyclic.Run</c> leaves no cycles, or that <c>NestingGraph.Run</c> connects a
/// disconnected graph). The layout itself never calls these, so they live in the test assembly rather than
/// widening the shipping library.
/// </summary>
static class GraphAlgorithms
{
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

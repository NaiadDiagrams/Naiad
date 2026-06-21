using Naiad.Dagre;

static class AddSubgraphConstraints
{
    public static void Run(Graph graph, Graph constraintGraph, List<string> vs)
    {
        var prev = new Dictionary<string, string>(StringComparer.Ordinal);
        string? rootPrev = null;

        foreach (var v in vs)
        {
            var child = graph.Parent(v);
            while (child != null)
            {
                var parent = graph.Parent(child);
                string? prevChild;
                if (parent != null)
                {
                    prevChild = prev.GetValueOrDefault(parent);
                    prev[parent] = child;
                }
                else
                {
                    prevChild = rootPrev;
                    rootPrev = child;
                }

                if (prevChild != null && prevChild != child)
                {
                    constraintGraph.SetEdge(prevChild, child);
                    break;
                }

                child = parent;
            }
        }
    }
}

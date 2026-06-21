namespace Naiad.Dagre;

/// <summary>Faithful port of dagre's <c>order/add-subgraph-constraints.ts</c>.</summary>
static class AddSubgraphConstraints
{
    public static void Run(Graph graph, Graph constraintGraph, List<string> vs)
    {
        var prev = new Dictionary<string, string>(StringComparer.Ordinal);
        string? rootPrev = null;

        foreach (var v in vs)
        {
            var child = graph.Parent(v);
            string? parent;
            string? prevChild;
            while (child != null)
            {
                parent = graph.Parent(child);
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

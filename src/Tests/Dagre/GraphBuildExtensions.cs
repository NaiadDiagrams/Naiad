namespace Naiad.Dagre;

/// <summary>
/// Test-only graph conveniences ported from graphlib's <c>Graph</c> API (<c>setNodes</c>/<c>setPath</c>/
/// <c>outEdges</c>). They are only ever used as scaffolding by the ported dagre tests, so they live in the
/// test assembly as extension methods rather than widening the shipping <see cref="Graph"/> surface that the
/// layout never calls.
/// </summary>
static class GraphBuildExtensions
{
    /// <summary>Out-edges of <paramref name="v"/>, optionally filtered to those ending at <paramref name="w"/>.
    /// Mirrors graphlib's directed <c>outEdges</c>: returns null when the node is absent.</summary>
    public static List<Edge>? OutEdges(this Graph graph, string v, string? w = null)
    {
        if (!graph.HasNode(v))
        {
            return null;
        }

        var edges = new List<Edge>();
        foreach (var e in graph.OutEdgesOf(v))
        {
            if (w == null || e.W == w)
            {
                edges.Add(e);
            }
        }

        return edges;
    }

    public static Graph SetNodes(this Graph graph, IEnumerable<string> names, NodeLabel? value = null)
    {
        foreach (var v in names)
        {
            if (value != null)
            {
                graph.SetNode(v, value);
            }
            else
            {
                graph.SetNode(v);
            }
        }

        return graph;
    }

    public static Graph SetPath(this Graph graph, IReadOnlyList<string> nodes, EdgeLabel? value = null)
    {
        for (var i = 0; i + 1 < nodes.Count; i++)
        {
            if (value != null)
            {
                graph.SetEdge(nodes[i], nodes[i + 1], value);
            }
            else
            {
                graph.SetEdge(nodes[i], nodes[i + 1]);
            }
        }

        return graph;
    }
}

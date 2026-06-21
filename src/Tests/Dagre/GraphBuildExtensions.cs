namespace Naiad.Dagre;

/// <summary>
/// Test-only graph builder conveniences ported from graphlib's <c>Graph</c> API (<c>setNodes</c>/<c>setPath</c>).
/// They are only ever used as scaffolding by the ported dagre tests, so they live in the test assembly as
/// extension methods rather than widening the shipping <see cref="Graph"/> surface that the layout never calls.
/// </summary>
static class GraphBuildExtensions
{
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

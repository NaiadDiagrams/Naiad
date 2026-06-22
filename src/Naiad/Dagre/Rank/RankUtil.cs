namespace Naiad.Dagre;

static class RankUtil
{
    /*
     * Initializes ranks for the input graph using the longest path algorithm. This
     * algorithm scales well and is fast in practice, it yields rather poor
     * solutions. Nodes are pushed to the lowest layer possible, leaving the bottom
     * ranks wide and leaving edges longer than necessary. However, due to its
     * speed, this algorithm is good for getting an initial ranking that can be fed
     * into other algorithms.
     *
     * This algorithm does not normalize layers because it will be used by other
     * algorithms in most cases. If using this algorithm directly, be sure to
     * run normalize at the end.
     *
     * Pre-conditions:
     *
     *    1. Input graph is a DAG.
     *    2. Input graph node labels can be assigned properties.
     *
     * Post-conditions:
     *
     *    1. Each node will be assign an (unnormalized) "rank" property.
     */
    public static void LongestPath(Graph graph)
    {
        var visited = new Dictionary<string, bool>(StringComparer.Ordinal);

        double Dfs(string v)
        {
            var label = graph.NodeLabel(v);
            if (visited.ContainsKey(v))
            {
                return label.Rank!.Value;
            }

            visited[v] = true;

            var rank = double.PositiveInfinity;
            foreach (var e in graph.OutEdgesOf(v))
            {
                var candidate = Dfs(e.W) - graph.FindEdgeLabel(e).Minlen!.Value;
                if (candidate < rank)
                {
                    rank = candidate;
                }
            }

            if (double.IsPositiveInfinity(rank))
            {
                rank = 0;
            }

            label.Rank = (int) rank;
            return label.Rank.Value;
        }

        foreach (var v in graph.Sources())
        {
            Dfs(v);
        }
    }

    /*
     * Returns the amount of slack for the given edge. The slack is defined as the
     * difference between the length of the edge and its minimum length.
     */
    public static int Slack(Graph graph, Edge edge) =>
        graph.NodeLabel(edge.W).Rank!.Value - graph.NodeLabel(edge.V).Rank!.Value - graph.FindEdgeLabel(edge).Minlen!.Value;
}

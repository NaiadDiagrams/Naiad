namespace Naiad.Dagre;

/// <summary>Faithful port of dagre's <c>order/init-order.ts</c>.</summary>
static class InitOrder
{
    public static List<List<string>> Run(Graph graph)
    {
        var visited = new Dictionary<string, bool>(StringComparer.Ordinal);
        var simpleNodes = graph.Nodes().Where(v => graph.Children(v).Count == 0).ToList();
        var simpleNodesRanks = simpleNodes.Select(v => (double) graph.Node(v).Rank!.Value).ToList();
        var maxRank = (int) Util.ApplyMax(simpleNodesRanks);
        var layers = Util.Range(maxRank + 1).Select(_ => new List<string>()).ToList();

        void Dfs(string v)
        {
            if (visited.GetValueOrDefault(v))
            {
                return;
            }

            visited[v] = true;
            var node = graph.Node(v);
            layers[node.Rank!.Value].Add(v);
            var successors = graph.Successors(v);
            if (successors != null)
            {
                foreach (var w in successors)
                {
                    Dfs(w);
                }
            }
        }

        // JS sort is stable; preserve original order for nodes of equal rank.
        var orderedVs = simpleNodes.OrderBy(v => graph.Node(v).Rank!.Value).ToList();
        foreach (var v in orderedVs)
        {
            Dfs(v);
        }

        return layers;
    }
}

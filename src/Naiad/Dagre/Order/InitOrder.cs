namespace Naiad.Dagre;

static class InitOrder
{
    public static List<List<string>> Run(Graph graph)
    {
        var visited = new Dictionary<string, bool>(StringComparer.Ordinal);
        var simpleNodes = graph.Nodes().Where(_ => graph.Children(_).Count == 0).ToList();
        var simpleNodesRanks = simpleNodes.Select(_ => (double) graph.NodeLabel(_).Rank!.Value).ToList();
        var maxRank = (int) Util.ApplyMax(simpleNodesRanks);
        var layers = Util.Range(maxRank + 1).Select(_ => new List<string>()).ToList();

        void Dfs(string visit)
        {
            if (visited.GetValueOrDefault(visit))
            {
                return;
            }

            visited[visit] = true;
            var node = graph.NodeLabel(visit);
            layers[node.Rank!.Value].Add(visit);
            var successors = graph.Successors(visit);
            if (successors != null)
            {
                foreach (var w in successors)
                {
                    Dfs(w);
                }
            }
        }

        // Stable sort: preserve original order for nodes of equal rank.
        var orderedVs = simpleNodes.OrderBy(_ => graph.NodeLabel(_).Rank!.Value).ToList();
        foreach (var v in orderedVs)
        {
            Dfs(v);
        }

        return layers;
    }
}

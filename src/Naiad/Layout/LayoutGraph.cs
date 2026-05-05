namespace Naiad.Layout;

class LayoutGraph
{
    public Dictionary<string, LayoutNode> Nodes { get; } = [];
    public List<LayoutEdge> Edges { get; } = [];
    public List<LayoutNode>[] Ranks { get; private set; } = [];

    public void AddNode(LayoutNode node) => Nodes[node.Id] = node;

    public void AddEdge(LayoutEdge edge)
    {
        Edges.Add(edge);
        if (Nodes.TryGetValue(edge.SourceId, out var source))
        {
            edge.Source = source;
            source.OutEdges.Add(edge);
        }
        if (Nodes.TryGetValue(edge.TargetId, out var target))
        {
            edge.Target = target;
            target.InEdges.Add(edge);
        }
    }

    public LayoutNode? GetNode(string id) =>
        Nodes.GetValueOrDefault(id);

    public IEnumerable<LayoutNode> GetSuccessors(string nodeId)
    {
        if (!Nodes.TryGetValue(nodeId, out var node))
        {
            yield break;
        }

        foreach (var edge in node.OutEdges)
        {
            if (Nodes.TryGetValue(edge.TargetId, out var target))
            {
                yield return target;
            }
        }
    }

    public IEnumerable<LayoutNode> GetPredecessors(string nodeId)
    {
        if (!Nodes.TryGetValue(nodeId, out var node))
        {
            yield break;
        }

        foreach (var edge in node.InEdges)
        {
            if (Nodes.TryGetValue(edge.SourceId, out var source))
            {
                yield return source;
            }
        }
    }

    public void BuildRanks()
    {
        var maxRank = Nodes.Values.Max(_ => _.Rank);
        Ranks = new List<LayoutNode>[maxRank + 1];

        for (var i = 0; i <= maxRank; i++)
        {
            Ranks[i] = [];
        }

        foreach (var node in Nodes.Values)
        {
            Ranks[node.Rank].Add(node);
        }
    }

    public void UpdateOrderInRanks()
    {
        foreach (var rank in Ranks)
        {
            rank.Sort((a, b) => a.Order.CompareTo(b.Order));
            for (var i = 0; i < rank.Count; i++)
            {
                rank[i].Order = i;
            }
        }
    }
}
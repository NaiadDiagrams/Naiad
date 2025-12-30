namespace MermaidSharp.Layout;

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
            source.OutEdges.Add(edge);
        }
        if (Nodes.TryGetValue(edge.TargetId, out var target))
        {
            target.InEdges.Add(edge);
        }
    }

    public LayoutNode? GetNode(string id) =>
        Nodes.GetValueOrDefault(id);

    public IEnumerable<LayoutNode> GetSuccessors(string nodeId)
    {
        if (!Nodes.TryGetValue(nodeId, out var node)) yield break;
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
        if (!Nodes.TryGetValue(nodeId, out var node)) yield break;
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
        var maxRank = Nodes.Values.Max(n => n.Rank);
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
        for (var r = 0; r < Ranks.Length; r++)
        {
            var nodesInRank = Ranks[r].OrderBy(n => n.Order).ToList();
            for (var i = 0; i < nodesInRank.Count; i++)
            {
                nodesInRank[i].Order = i;
            }
            Ranks[r] = nodesInRank;
        }
    }
}

internal class LayoutNode
{
    public required string Id { get; init; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int Rank { get; set; } = -1;
    public int Order { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public bool IsDummy { get; set; }
    public string? OriginalEdgeSource { get; set; }
    public string? OriginalEdgeTarget { get; set; }

    public List<LayoutEdge> InEdges { get; } = [];
    public List<LayoutEdge> OutEdges { get; } = [];
}

internal class LayoutEdge
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public int Weight { get; set; } = 1;
    public bool IsReversed { get; set; }
    public List<MermaidSharp.Models.Position> Points { get; } = [];
}

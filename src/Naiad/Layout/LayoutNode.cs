class LayoutNode
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
class LayoutNode
{
    public required string Id { get; init; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int Rank { get; set; } = -1;
    public int Order { get; set; }

    // Transient scratch for the ordering sweep: the median neighbour position used to sort a rank. Held
    // on the node so the hot sort comparator reads a field instead of a string-keyed dictionary lookup.
    public double MedianPosition { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public bool IsDummy { get; set; }
    public string? OriginalEdgeSource { get; set; }
    public string? OriginalEdgeTarget { get; set; }

    public List<LayoutEdge> InEdges { get; } = [];
    public List<LayoutEdge> OutEdges { get; } = [];
}
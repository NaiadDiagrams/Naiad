class LayoutEdge
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public LayoutNode? Source { get; set; }
    public LayoutNode? Target { get; set; }
    public int Weight { get; set; } = 1;
    public bool IsReversed { get; set; }
    public List<Position> Points { get; } = [];
}
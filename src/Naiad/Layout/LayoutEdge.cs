class LayoutEdge
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public int Weight { get; set; } = 1;
    public bool IsReversed { get; set; }
    public List<Position> Points { get; } = [];
}
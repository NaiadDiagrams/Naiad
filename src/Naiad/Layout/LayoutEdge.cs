class LayoutEdge
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public LayoutNode? Source { get; set; }
    public LayoutNode? Target { get; set; }
    public bool IsReversed { get; set; }
    public RankConstraint RankConstraint { get; set; }
    public double LabelWidth { get; set; }
    public double LabelHeight { get; set; }
    public List<Position> Points { get; } = [];

    public bool IsSameRank =>
        RankConstraint is
            RankConstraint.Same or
            RankConstraint.SameBefore or
            RankConstraint.SameAfter;
}
namespace MermaidSharp;

public class LayoutOptions
{
    public static LayoutOptions Default => new();

    public Direction Direction { get; set; } = Direction.TopToBottom;
    public double NodeSeparation { get; set; } = 50;
    public double RankSeparation { get; set; } = 50;
    public double EdgeSeparation { get; set; } = 10;
    public double MarginX { get; set; } = 8;
    public double MarginY { get; set; } = 8;
    public RankerType Ranker { get; set; } = RankerType.TightTree;
}
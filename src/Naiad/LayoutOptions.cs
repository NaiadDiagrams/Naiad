namespace Naiad;

public class LayoutOptions
{
    public Direction Direction { get; set; } = Direction.TopToBottom;
    public double NodeSeparation { get; set; } = 50;
    public double RankSeparation { get; set; } = 50;
    public RankerType Ranker { get; set; } = RankerType.TightTree;
}

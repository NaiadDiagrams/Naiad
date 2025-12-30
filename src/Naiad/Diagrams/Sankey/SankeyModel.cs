namespace MermaidSharp.Diagrams.Sankey;

public class SankeyModel : DiagramBase
{
    public List<SankeyLink> Links { get; } = [];
}

public class SankeyLink
{
    public required string Source { get; init; }
    public required string Target { get; init; }
    public double Value { get; init; }
}

public class SankeyNode
{
    public required string Name { get; init; }
    public int Column { get; set; }
    public double Y { get; set; }
    public double Height { get; set; }
    public double InputValue { get; set; }
    public double OutputValue { get; set; }
}

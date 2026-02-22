namespace MermaidSharp.Diagrams.Sankey;

public class SankeyLink
{
    public required string Source { get; init; }
    public required string Target { get; init; }
    public double Value { get; init; }
}
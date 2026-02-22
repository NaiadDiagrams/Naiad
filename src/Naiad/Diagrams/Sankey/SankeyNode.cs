namespace MermaidSharp.Diagrams.Sankey;

public class SankeyNode
{
    public required string Name { get; init; }
    public int Column { get; set; }
    public double Y { get; set; }
    public double Height { get; set; }
    public double InputValue { get; set; }
    public double OutputValue { get; set; }
}
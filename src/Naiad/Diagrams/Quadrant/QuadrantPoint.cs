namespace MermaidSharp.Diagrams.Quadrant;

public class QuadrantPoint
{
    public required string Name { get; init; }
    public double X { get; init; } // 0.0 to 1.0
    public double Y { get; init; } // 0.0 to 1.0
}
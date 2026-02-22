namespace MermaidSharp.Diagrams.Pie;

public class PieSection
{
    public required string Label { get; init; }
    public double Value { get; init; }
    public string? Color { get; set; }
}
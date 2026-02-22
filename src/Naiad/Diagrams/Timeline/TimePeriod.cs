namespace MermaidSharp.Diagrams.Timeline;

public class TimePeriod
{
    public required string Label { get; init; }
    public List<string> Events { get; } = [];

    // Layout properties
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
}
namespace MermaidSharp.Diagrams.Radar;

public class RadarCurve
{
    public required string Id { get; init; }
    public string? Label { get; set; }
    public List<double> Values { get; } = [];
    public Dictionary<string, double> NamedValues { get; } = [];
}
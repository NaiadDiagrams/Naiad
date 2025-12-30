namespace MermaidSharp.Diagrams.Pie;

public class PieModel : DiagramBase
{
    public bool ShowData { get; set; }
    public List<PieSection> Sections { get; } = [];
}

public class PieSection
{
    public required string Label { get; init; }
    public double Value { get; init; }
    public string? Color { get; set; }
}

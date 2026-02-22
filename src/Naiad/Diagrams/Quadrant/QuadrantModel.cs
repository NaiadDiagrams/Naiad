namespace MermaidSharp.Diagrams.Quadrant;

public class QuadrantModel : DiagramBase
{
    public string? XAxisLeft { get; set; }
    public string? XAxisRight { get; set; }
    public string? YAxisBottom { get; set; }
    public string? YAxisTop { get; set; }
    public string? Quadrant1Label { get; set; } // Top-right
    public string? Quadrant2Label { get; set; } // Top-left
    public string? Quadrant3Label { get; set; } // Bottom-left
    public string? Quadrant4Label { get; set; } // Bottom-right
    public List<QuadrantPoint> Points { get; } = [];
}
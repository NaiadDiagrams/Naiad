namespace MermaidSharp.Diagrams.Mindmap;

public class MindmapNode
{
    public required string Text { get; init; }
    public MindmapShape Shape { get; set; } = MindmapShape.Default;
    public string? Icon { get; set; }
    public string? CssClass { get; set; }
    public List<MindmapNode> Children { get; } = [];
    public int Level { get; set; }

    // Layout properties
    public Position Position { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double SubtreeHeight { get; set; }
}
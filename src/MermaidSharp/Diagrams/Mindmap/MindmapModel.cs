using MermaidSharp.Models;

namespace MermaidSharp.Diagrams.Mindmap;

public class MindmapModel : DiagramBase
{
    public MindmapNode? Root { get; set; }
}

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

public enum MindmapShape
{
    Default,      // No brackets - rectangle with rounded corners
    Square,       // [text] - square
    Rounded,      // (text) - rounded rectangle
    Circle,       // ((text)) - circle
    Bang,         // ))text(( - cloud/explosion
    Cloud,        // )text( - cloud
    Hexagon       // {{text}} - hexagon
}

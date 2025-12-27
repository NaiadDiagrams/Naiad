namespace MermaidSharp.Core.Models;

public class Node
{
    public required string Id { get; init; }
    public string? Label { get; set; }
    public NodeShape Shape { get; set; } = NodeShape.Rectangle;
    public double Width { get; set; }
    public double Height { get; set; }
    public Position Position { get; set; }
    public Style Style { get; set; } = new();
    public string? CssClass { get; set; }
    public string? Link { get; set; }
    public string? Tooltip { get; set; }
    public string? ParentId { get; set; }
    public bool IsGroup { get; set; }

    // Layout properties (set by layout engine)
    public int Rank { get; set; }
    public int Order { get; set; }

    public string DisplayLabel => Label ?? Id;

    public Rect Bounds => new(
        Position.X - Width / 2,
        Position.Y - Height / 2,
        Width,
        Height);
}

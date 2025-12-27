namespace MermaidSharp.Models;

public class Subgraph
{
    public required string Id { get; init; }
    public string? Title { get; set; }
    public Direction Direction { get; set; } = Direction.TopToBottom;
    public List<string> NodeIds { get; } = [];
    public List<Subgraph> NestedSubgraphs { get; } = [];

    // Layout properties
    public Position Position { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Rect Bounds => new(
        Position.X - Width / 2,
        Position.Y - Height / 2,
        Width,
        Height);
}
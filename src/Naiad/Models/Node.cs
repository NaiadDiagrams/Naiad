namespace Naiad.Models;

public class Node
{
    public required string Id { get; init; }
    public string? Label { get; set; }
    public NodeShape Shape { get; set; } = NodeShape.Rectangle;
    public double Width { get; set; }
    public double Height { get; set; }
    public Position Position { get; set; }
}

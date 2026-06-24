namespace Naiad;

public class Node
{
    public required string Id { get; init; }
    public string? Label { get; set; }
    public NodeShape Shape { get; set; } = NodeShape.Rectangle;
    public double Width { get; set; }
    public double Height { get; set; }
    public Position Position { get; set; }

    /// <summary>Style classes assigned via <c>:::name</c> shorthand or a <c>class</c> directive.</summary>
    public List<string> Classes { get; } = [];

    /// <summary>Resolved visual overrides (from <c>classDef</c>/<c>class</c>/<c>style</c>), or null for theme defaults.</summary>
    public NodeStyle? Style { get; set; }
}

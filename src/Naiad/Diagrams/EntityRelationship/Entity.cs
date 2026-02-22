namespace MermaidSharp.Diagrams.EntityRelationship;

public class Entity
{
    public required string Name { get; init; }
    public List<EntityAttribute> Attributes { get; } = [];

    // Layout properties
    public Position Position { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
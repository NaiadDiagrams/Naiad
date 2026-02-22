namespace MermaidSharp.Diagrams.EntityRelationship;

public class EntityAttribute
{
    public required string Name { get; init; }
    public string? Type { get; set; }
    public AttributeKeyType KeyType { get; set; } = AttributeKeyType.None;
    public string? Comment { get; set; }
}
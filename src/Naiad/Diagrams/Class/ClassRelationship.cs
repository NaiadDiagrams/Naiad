namespace MermaidSharp.Diagrams.Class;

public class ClassRelationship
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public RelationshipType Type { get; set; } = RelationshipType.Association;
    public string? Label { get; set; }
    public string? FromCardinality { get; set; }
    public string? ToCardinality { get; set; }
}
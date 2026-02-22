namespace MermaidSharp.Diagrams.EntityRelationship;

public class Relationship
{
    public required string FromEntity { get; init; }
    public required string ToEntity { get; init; }
    public Cardinality FromCardinality { get; set; } = Cardinality.ExactlyOne;
    public Cardinality ToCardinality { get; set; } = Cardinality.ExactlyOne;
    public string? Label { get; set; }
    public bool Identifying { get; set; } = true; // solid vs dashed line
}
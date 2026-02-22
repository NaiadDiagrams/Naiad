namespace MermaidSharp.Diagrams.EntityRelationship;

public class ERModel : DiagramBase
{
    public List<Entity> Entities { get; } = [];
    public List<Relationship> Relationships { get; } = [];
}
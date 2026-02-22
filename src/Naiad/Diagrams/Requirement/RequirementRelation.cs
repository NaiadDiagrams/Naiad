namespace MermaidSharp.Diagrams.Requirement;

public class RequirementRelation
{
    public required string Source { get; init; }
    public required string Target { get; init; }
    public RelationType Type { get; set; } = RelationType.Satisfies;
}
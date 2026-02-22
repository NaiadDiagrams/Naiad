namespace MermaidSharp.Diagrams.Requirement;

public class RequirementModel : DiagramBase
{
    public List<Requirement> Requirements { get; } = [];
    public List<RequirementElement> Elements { get; } = [];
    public List<RequirementRelation> Relations { get; } = [];
}
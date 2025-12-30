namespace MermaidSharp.Diagrams.Requirement;

public class RequirementModel : DiagramBase
{
    public List<Requirement> Requirements { get; } = [];
    public List<RequirementElement> Elements { get; } = [];
    public List<RequirementRelation> Relations { get; } = [];
}

public class Requirement
{
    public string Id { get; set; } = "";
    public required string Name { get; init; }
    public string? Text { get; set; }
    public RequirementType Type { get; set; } = RequirementType.Requirement;
    public RiskLevel Risk { get; set; } = RiskLevel.Medium;
    public VerifyMethod VerifyMethod { get; set; } = VerifyMethod.Test;
}

public enum RequirementType
{
    Requirement,
    FunctionalRequirement,
    InterfaceRequirement,
    PerformanceRequirement,
    PhysicalRequirement,
    DesignConstraint
}

public enum RiskLevel
{
    Low,
    Medium,
    High
}

public enum VerifyMethod
{
    Analysis,
    Demonstration,
    Inspection,
    Test
}

public class RequirementElement
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Type { get; set; }
    public string? DocRef { get; set; }
}

public class RequirementRelation
{
    public required string Source { get; init; }
    public required string Target { get; init; }
    public RelationType Type { get; set; } = RelationType.Satisfies;
}

public enum RelationType
{
    Contains,
    Copies,
    Derives,
    Satisfies,
    Verifies,
    Refines,
    Traces
}

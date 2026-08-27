namespace Naiad.Diagrams.Requirement;

public class Requirement
{
    /// <summary>The declared <c>id:</c>, or null when the diagram gives none.</summary>
    public string? Id { get; set; }

    public required string Name { get; init; }
    public string? Text { get; set; }
    public RequirementType Type { get; set; } = RequirementType.Requirement;

    // Null when undeclared. These have no sensible default: rendering a level the author never wrote
    // states something about the requirement that the diagram does not say.
    public RiskLevel? Risk { get; set; }
    public VerifyMethod? VerifyMethod { get; set; }
}

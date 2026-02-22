namespace MermaidSharp.Diagrams.Requirement;

public class Requirement
{
    public string Id { get; set; } = "";
    public required string Name { get; init; }
    public string? Text { get; set; }
    public RequirementType Type { get; set; } = RequirementType.Requirement;
    public RiskLevel Risk { get; set; } = RiskLevel.Medium;
    public VerifyMethod VerifyMethod { get; set; } = VerifyMethod.Test;
}
namespace MermaidSharp.Diagrams.Requirement;

public class RequirementElement
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Type { get; set; }
    public string? DocRef { get; set; }
}
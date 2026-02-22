namespace MermaidSharp.Diagrams.Architecture;

public class ArchitectureGroup
{
    public required string Id { get; init; }
    public string? Icon { get; set; }
    public string? Label { get; set; }
    public string? Parent { get; set; }
}
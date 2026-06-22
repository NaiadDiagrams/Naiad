namespace Naiad.Diagrams.Architecture;

public class ArchitectureGroup
{
    public required string Id { get; init; }
    public string? Icon { get; init; }
    public string? Label { get; init; }
    public string? Parent { get; set; }
}
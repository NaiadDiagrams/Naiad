namespace MermaidSharp.Diagrams.C4;

public class C4Relationship
{
    public required string From { get; init; }
    public required string To { get; init; }
    public string? Label { get; set; }
    public string? Technology { get; set; }
}
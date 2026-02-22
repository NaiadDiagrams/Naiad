namespace MermaidSharp.Diagrams.C4;

public class C4Element
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public string? Description { get; set; }
    public string? Technology { get; set; }
    public C4ElementType Type { get; set; } = C4ElementType.System;
    public bool IsExternal { get; set; }
    public string? BoundaryId { get; set; }
}
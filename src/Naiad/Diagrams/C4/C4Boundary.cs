namespace MermaidSharp.Diagrams.C4;

public class C4Boundary
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public C4BoundaryType Type { get; set; } = C4BoundaryType.System;
    public string? ParentBoundaryId { get; set; }
    public List<string> ElementIds { get; } = [];
    public List<string> ChildBoundaryIds { get; } = [];
}
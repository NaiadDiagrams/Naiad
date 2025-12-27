using MermaidSharp.Models;

namespace MermaidSharp.Diagrams.Architecture;

public class ArchitectureModel : DiagramBase
{
    public List<ArchitectureGroup> Groups { get; } = [];
    public List<ArchitectureService> Services { get; } = [];
    public List<ArchitectureEdge> Edges { get; } = [];
    public List<ArchitectureJunction> Junctions { get; } = [];
}

public class ArchitectureGroup
{
    public required string Id { get; init; }
    public string? Icon { get; set; }
    public string? Label { get; set; }
    public string? Parent { get; set; }
}

public class ArchitectureService
{
    public required string Id { get; init; }
    public string? Icon { get; set; }
    public string? Label { get; set; }
    public string? Parent { get; set; }
}

public class ArchitectureJunction
{
    public required string Id { get; init; }
    public string? Parent { get; set; }
}

public class ArchitectureEdge
{
    public required string SourceId { get; init; }
    public required string TargetId { get; init; }
    public EdgeDirection SourceSide { get; set; } = EdgeDirection.Right;
    public EdgeDirection TargetSide { get; set; } = EdgeDirection.Left;
    public bool SourceArrow { get; set; }
    public bool TargetArrow { get; set; }
    public string? SourceGroup { get; set; }
    public string? TargetGroup { get; set; }
}

public enum EdgeDirection
{
    Left,
    Right,
    Top,
    Bottom
}

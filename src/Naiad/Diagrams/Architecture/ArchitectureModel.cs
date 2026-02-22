namespace MermaidSharp.Diagrams.Architecture;

public class ArchitectureModel : DiagramBase
{
    public List<ArchitectureGroup> Groups { get; } = [];
    public List<ArchitectureService> Services { get; } = [];
    public List<ArchitectureEdge> Edges { get; } = [];
    public List<ArchitectureJunction> Junctions { get; } = [];
}
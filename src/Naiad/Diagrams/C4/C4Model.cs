namespace MermaidSharp.Diagrams.C4;

public class C4Model : DiagramBase
{
    public C4DiagramType Type { get; set; } = C4DiagramType.Context;
    public List<C4Element> Elements { get; } = [];
    public List<C4Relationship> Relationships { get; } = [];
    public List<C4Boundary> Boundaries { get; } = [];
}
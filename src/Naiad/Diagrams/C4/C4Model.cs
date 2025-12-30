namespace MermaidSharp.Diagrams.C4;

public class C4Model : DiagramBase
{
    public C4DiagramType Type { get; set; } = C4DiagramType.Context;
    public List<C4Element> Elements { get; } = [];
    public List<C4Relationship> Relationships { get; } = [];
    public List<C4Boundary> Boundaries { get; } = [];
}

public enum C4DiagramType
{
    Context,
    Container,
    Component,
    Deployment
}

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

public enum C4ElementType
{
    Person,
    System,
    Container,
    Component,
    ContainerDb,
    ContainerQueue,
    Node,
    NodeL,
    NodeR
}

public class C4Relationship
{
    public required string From { get; init; }
    public required string To { get; init; }
    public string? Label { get; set; }
    public string? Technology { get; set; }
}

public class C4Boundary
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public C4BoundaryType Type { get; set; } = C4BoundaryType.System;
    public List<string> ElementIds { get; } = [];
}

public enum C4BoundaryType
{
    System,
    Container,
    Enterprise,
    Deployment,
    Node
}

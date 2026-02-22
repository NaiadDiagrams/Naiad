namespace MermaidSharp.Diagrams.Class;

public class ClassDefinition
{
    public required string Id { get; init; }
    public string? DisplayName { get; set; }
    public List<ClassMember> Members { get; } = [];
    public List<ClassMethod> Methods { get; } = [];
    public ClassAnnotation? Annotation { get; set; }
    public string? Namespace { get; set; }
    public string? CssClass { get; set; }

    public string Name => DisplayName ?? Id;
}
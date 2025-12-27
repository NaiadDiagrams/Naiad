using MermaidSharp.Models;

namespace MermaidSharp.Diagrams.Class;

public class ClassModel : DiagramBase
{
    public List<ClassDefinition> Classes { get; } = [];
    public List<ClassRelationship> Relationships { get; } = [];
}

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

public class ClassMember
{
    public required string Name { get; init; }
    public string? Type { get; set; }
    public Visibility Visibility { get; set; } = Visibility.Public;
    public bool IsStatic { get; set; }
}

public class ClassMethod
{
    public required string Name { get; init; }
    public string? ReturnType { get; set; }
    public List<MethodParameter> Parameters { get; } = [];
    public Visibility Visibility { get; set; } = Visibility.Public;
    public bool IsStatic { get; set; }
    public bool IsAbstract { get; set; }
}

public class MethodParameter
{
    public required string Name { get; init; }
    public string? Type { get; set; }
}

public enum Visibility
{
    Public,      // +
    Private,     // -
    Protected,   // #
    PackagePrivate // ~
}

public enum ClassAnnotation
{
    Interface,
    Abstract,
    Service,
    Enumeration
}

public class ClassRelationship
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public RelationshipType Type { get; set; } = RelationshipType.Association;
    public string? Label { get; set; }
    public string? FromCardinality { get; set; }
    public string? ToCardinality { get; set; }
}

public enum RelationshipType
{
    Inheritance,      // <|--
    Composition,      // *--
    Aggregation,      // o--
    Association,      // -->
    DependencyLeft,   // ..>
    DependencyRight,  // <..
    Realization,      // ..|>
    Link              // --
}

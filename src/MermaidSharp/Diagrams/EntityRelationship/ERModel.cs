namespace MermaidSharp.Diagrams.EntityRelationship;

public class ERModel : DiagramBase
{
    public List<Entity> Entities { get; } = [];
    public List<Relationship> Relationships { get; } = [];
}

public class Entity
{
    public required string Name { get; init; }
    public List<EntityAttribute> Attributes { get; } = [];

    // Layout properties
    public Position Position { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class EntityAttribute
{
    public required string Name { get; init; }
    public string? Type { get; set; }
    public AttributeKeyType KeyType { get; set; } = AttributeKeyType.None;
    public string? Comment { get; set; }
}

public enum AttributeKeyType
{
    None,
    PrimaryKey,  // PK
    ForeignKey,  // FK
    UniqueKey    // UK
}

public class Relationship
{
    public required string FromEntity { get; init; }
    public required string ToEntity { get; init; }
    public Cardinality FromCardinality { get; set; } = Cardinality.ExactlyOne;
    public Cardinality ToCardinality { get; set; } = Cardinality.ExactlyOne;
    public string? Label { get; set; }
    public bool Identifying { get; set; } = true; // solid vs dashed line
}

public enum Cardinality
{
    ExactlyOne,   // ||
    ZeroOrOne,    // |o or o|
    OneOrMore,    // }| or |{
    ZeroOrMore    // }o or o{
}

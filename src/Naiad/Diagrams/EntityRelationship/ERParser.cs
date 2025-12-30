namespace MermaidSharp.Diagrams.EntityRelationship;

public class ERParser : IDiagramParser<ERModel>
{
    public DiagramType DiagramType => DiagramType.EntityRelationship;

    // Entity name (alphanumeric, underscore, hyphen)
    static readonly Parser<char, string> EntityName =
        Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
            .AtLeastOnceString()
            .Labelled("entity name");

    // Left cardinality markers
    static readonly Parser<char, Cardinality> LeftCardinality =
        OneOf(
            Try(String("||")).ThenReturn(Cardinality.ExactlyOne),
            Try(String("|o")).ThenReturn(Cardinality.ZeroOrOne),
            Try(String("}|")).ThenReturn(Cardinality.OneOrMore),
            String("}o").ThenReturn(Cardinality.ZeroOrMore)
        );

    // Right cardinality markers
    static readonly Parser<char, Cardinality> RightCardinality =
        OneOf(
            Try(String("||")).ThenReturn(Cardinality.ExactlyOne),
            Try(String("o|")).ThenReturn(Cardinality.ZeroOrOne),
            Try(String("|{")).ThenReturn(Cardinality.OneOrMore),
            String("o{").ThenReturn(Cardinality.ZeroOrMore)
        );

    // Line style (-- for identifying, .. for non-identifying)
    static readonly Parser<char, bool> LineStyle =
        OneOf(
            String("--").ThenReturn(true),
            String("..").ThenReturn(false)
        );

    // Relationship: ENTITY1 ||--o{ ENTITY2 : label
    static readonly Parser<char, Relationship> RelationshipParser =
        from _ in CommonParsers.InlineWhitespace
        from fromEntity in EntityName
        from __ in CommonParsers.InlineWhitespace
        from leftCard in LeftCardinality
        from identifying in LineStyle
        from rightCard in RightCardinality
        from ___ in CommonParsers.InlineWhitespace
        from toEntity in EntityName
        from label in Try(
            CommonParsers.InlineWhitespace
                .Then(Char(':'))
                .Then(CommonParsers.InlineWhitespace)
                .Then(Token(c => c != '\r' && c != '\n').AtLeastOnceString())
        ).Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new Relationship
        {
            FromEntity = fromEntity,
            ToEntity = toEntity,
            FromCardinality = leftCard,
            ToCardinality = rightCard,
            Label = label.HasValue ? label.Value.Trim() : null,
            Identifying = identifying
        };

    // Attribute key type
    static readonly Parser<char, AttributeKeyType> KeyTypeParser =
        OneOf(
            Try(String("PK")).ThenReturn(AttributeKeyType.PrimaryKey),
            Try(String("FK")).ThenReturn(AttributeKeyType.ForeignKey),
            String("UK").ThenReturn(AttributeKeyType.UniqueKey)
        );

    // Attribute comment (in quotes)
    static readonly Parser<char, string> AttributeComment =
        CommonParsers.DoubleQuotedString;

    // Entity attribute: type name PK "comment"
    static readonly Parser<char, EntityAttribute> AttributeParser =
        from _ in CommonParsers.InlineWhitespace
        from type in Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '[' || c == ']').AtLeastOnceString()
        from __ in CommonParsers.RequiredWhitespace
        from name in Token(c => char.IsLetterOrDigit(c) || c == '_').AtLeastOnceString()
        from ___ in CommonParsers.InlineWhitespace
        from keyType in Try(KeyTypeParser).Optional()
        from ____ in CommonParsers.InlineWhitespace
        from comment in Try(AttributeComment).Optional()
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new EntityAttribute
        {
            Name = name,
            Type = type,
            KeyType = keyType.HasValue ? keyType.Value : AttributeKeyType.None,
            Comment = comment.HasValue ? comment.Value : null
        };

    // Entity body content: individual attribute lines
    static Parser<char, List<EntityAttribute>> EntityBodyParser()
    {
        var attributeOrEmpty = OneOf(
            Try(AttributeParser.Select(a => (EntityAttribute?)a)),
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.LineEnd)).ThenReturn((EntityAttribute?)null)
        );

        return attributeOrEmpty.Many()
            .Select(attrs => attrs.Where(a => a != null).Cast<EntityAttribute>().ToList());
    }

    // Entity definition: EntityName { attributes }
    static Parser<char, Entity> EntityDefinitionParser =>
        Try(
            from _ in CommonParsers.InlineWhitespace
            from name in EntityName
            from __ in CommonParsers.InlineWhitespace
            from open in Char('{')
            from ___ in CommonParsers.LineEnd
            from attributes in EntityBodyParser()
            from ____ in CommonParsers.InlineWhitespace
            from close in Char('}')
            from _____ in CommonParsers.LineEnd
            select CreateEntity(name, attributes)
        );

    static Entity CreateEntity(string name, List<EntityAttribute> attributes)
    {
        var entity = new Entity { Name = name };
        entity.Attributes.AddRange(attributes);
        return entity;
    }

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> SkipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    public static Parser<char, ERModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("erDiagram")
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        from content in ParseContent()
        select BuildModel(content);

    static Parser<char, List<object>> ParseContent()
    {
        var element = OneOf(
            Try(EntityDefinitionParser.Select(e => (object)e)),
            Try(RelationshipParser.Select(r => (object)r)),
            SkipLine.ThenReturn((object)Unit.Value)
        );

        return element.Many().Select(e => e.Where(x => x is not Unit).ToList());
    }

    static ERModel BuildModel(List<object> content)
    {
        var model = new ERModel();
        var entityMap = new Dictionary<string, Entity>();

        foreach (var item in content)
        {
            switch (item)
            {
                case Entity e:
                    if (entityMap.TryGetValue(e.Name, out var existing))
                    {
                        // Merge attributes into existing entity
                        existing.Attributes.AddRange(e.Attributes);
                    }
                    else
                    {
                        entityMap[e.Name] = e;
                        model.Entities.Add(e);
                    }

                    break;

                case Relationship r:
                    // Auto-create entities from relationships
                    EnsureEntity(r.FromEntity, entityMap, model);
                    EnsureEntity(r.ToEntity, entityMap, model);
                    model.Relationships.Add(r);
                    break;
            }
        }

        return model;
    }

    static void EnsureEntity(string name, Dictionary<string, Entity> entityMap, ERModel model)
    {
        if (entityMap.ContainsKey(name))
            return;

        var entity = new Entity { Name = name };
        entityMap[name] = entity;
        model.Entities.Add(entity);
    }

    public Result<char, ERModel> Parse(string input) => Parser.Parse(input);
}

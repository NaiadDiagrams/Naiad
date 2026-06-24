class ERParser : IDiagramParser<ERModel>
{
    // Entity name (alphanumeric, underscore, hyphen)
    static readonly Parser<char, string> entityName;

    // Left cardinality markers
    static readonly Parser<char, Cardinality> leftCardinality;

    // Right cardinality markers
    static readonly Parser<char, Cardinality> rightCardinality;

    // Line style (-- for identifying, .. for non-identifying)
    static readonly Parser<char, bool> lineStyle;

    // Relationship: ENTITY1 ||--o{ ENTITY2 : label
    static readonly Parser<char, Relationship> relationshipParser;

    // Attribute key type
    static readonly Parser<char, AttributeKeyType> keyTypeParser;

    // Attribute comment (in quotes)
    static readonly Parser<char, string> attributeComment;

    // Entity attribute: type name PK "comment"
    static readonly Parser<char, EntityAttribute> attributeParser;

    // Entity body content: individual attribute lines
    static readonly Parser<char, List<EntityAttribute>> EntityBodyParser;

    // Entity definition: EntityName { attributes }
    static readonly Parser<char, Entity> EntityDefinitionParser;

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine;

    static readonly Parser<char, IEnumerable<IERContent?>> ParseContent;

    static readonly Parser<char, ERModel> Parser;

    static ERParser()
    {
        entityName =
            Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-')
                .AtLeastOnceString()
                .Labelled("entity name");

        leftCardinality =
            OneOf(
                Try(String("||")).ThenReturn(Cardinality.ExactlyOne),
                Try(String("|o")).ThenReturn(Cardinality.ZeroOrOne),
                Try(String("}|")).ThenReturn(Cardinality.OneOrMore),
                String("}o").ThenReturn(Cardinality.ZeroOrMore)
            );

        rightCardinality =
            OneOf(
                Try(String("||")).ThenReturn(Cardinality.ExactlyOne),
                Try(String("o|")).ThenReturn(Cardinality.ZeroOrOne),
                Try(String("|{")).ThenReturn(Cardinality.OneOrMore),
                String("o{").ThenReturn(Cardinality.ZeroOrMore)
            );

        lineStyle =
            OneOf(
                String("--").ThenReturn(true),
                String("..").ThenReturn(false)
            );

        relationshipParser =
            from _ in CommonParsers.InlineWhitespace
            from fromEntity in entityName
            from __ in CommonParsers.InlineWhitespace
            from leftCard in leftCardinality
            from identifying in lineStyle
            from rightCard in rightCardinality
            from ___ in CommonParsers.InlineWhitespace
            from toEntity in entityName
            from label in Try(
                CommonParsers.InlineWhitespace
                    .Then(Char(':'))
                    .Then(CommonParsers.InlineWhitespace)
                    .Then(Token(_ => _ != '\r' && _ != '\n').AtLeastOnceString())
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

        keyTypeParser =
            OneOf(
                Try(String("PK")).ThenReturn(AttributeKeyType.PrimaryKey),
                Try(String("FK")).ThenReturn(AttributeKeyType.ForeignKey),
                String("UK").ThenReturn(AttributeKeyType.UniqueKey)
            );

        attributeComment =
            CommonParsers.DoubleQuotedString;

        attributeParser =
            from _ in CommonParsers.InlineWhitespace
            from type in Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '[' || _ == ']').AtLeastOnceString()
            from __ in CommonParsers.RequiredWhitespace
            from name in Token(_ => char.IsLetterOrDigit(_) || _ == '_').AtLeastOnceString()
            from ___ in CommonParsers.InlineWhitespace
            from keyType in Try(keyTypeParser).Optional()
            from ____ in CommonParsers.InlineWhitespace
            from comment in Try(attributeComment).Optional()
            from _____ in CommonParsers.InlineWhitespace
            from lineEnd in CommonParsers.LineEnd
            select new EntityAttribute
            {
                Name = name,
                Type = type,
                KeyType = keyType.HasValue ? keyType.Value : AttributeKeyType.None,
                Comment = comment.HasValue ? comment.Value : null
            };

        EntityBodyParser =
            OneOf(
                Try(attributeParser.Select<EntityAttribute?>(_ => _)),
                Try(CommonParsers.InlineWhitespace.Then(CommonParsers.LineEnd))
                    .ThenReturn<EntityAttribute?>(null)
            ).Many()
            .Select(_ => _.Where(_ => _ != null).Cast<EntityAttribute>().ToList());

        EntityDefinitionParser =
            Try(
                from _ in CommonParsers.InlineWhitespace
                from name in entityName
                from __ in CommonParsers.InlineWhitespace
                from open in Char('{')
                from ___ in CommonParsers.LineEnd
                from attributes in EntityBodyParser
                from ____ in CommonParsers.InlineWhitespace
                from close in Char('}')
                from _____ in CommonParsers.LineEnd
                select CreateEntity(name, attributes)
            );

        skipLine =
            CommonParsers.InlineWhitespace
                .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

        ParseContent =
            OneOf(
                Try(EntityDefinitionParser.Select<IERContent?>(_ => new EntityItem(_))),
                Try(relationshipParser.Select<IERContent?>(_ => new RelationshipItem(_))),
                skipLine.ThenReturn<IERContent?>(null)
            ).Many();

        Parser =
            from _ in CommonParsers.InlineWhitespace
            from keyword in String("erDiagram")
            from __ in CommonParsers.InlineWhitespace
            from ___ in CommonParsers.LineEnd
            from content in ParseContent
            select BuildModel(content);
    }

    static Entity CreateEntity(string name, List<EntityAttribute> attributes)
    {
        var entity = new Entity { Name = name };
        entity.Attributes.AddRange(attributes);
        return entity;
    }

    static ERModel BuildModel(IEnumerable<IERContent?> content)
    {
        var model = new ERModel();
        var entityMap = new Dictionary<string, Entity>();

        foreach (var item in content)
        {
            switch (item)
            {
                case EntityItem entity:
                    var e = entity.Value;
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

                case RelationshipItem rel:
                    var r = rel.Value;
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

    internal interface IERContent;
    readonly record struct EntityItem(Entity Value) : IERContent;
    readonly record struct RelationshipItem(Relationship Value) : IERContent;
}

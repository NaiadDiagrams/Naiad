class RequirementParser : IDiagramParser<RequirementModel>
{
    static Parser<char, string> identifier =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

    static Parser<char, string> restOfLine =
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    static Parser<char, RequirementType> requirementTypeParser =
        OneOf(
            Try(CIString("functionalRequirement")).ThenReturn(RequirementType.FunctionalRequirement),
            Try(CIString("interfaceRequirement")).ThenReturn(RequirementType.InterfaceRequirement),
            Try(CIString("performanceRequirement")).ThenReturn(RequirementType.PerformanceRequirement),
            Try(CIString("physicalRequirement")).ThenReturn(RequirementType.PhysicalRequirement),
            Try(CIString("designConstraint")).ThenReturn(RequirementType.DesignConstraint),
            CIString("requirement").ThenReturn(RequirementType.Requirement)
        );

    // Property: key: value
    static Parser<char, (string key, string value)> propertyParser =
        from _ in CommonParsers.InlineWhitespace
        from key in identifier
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from value in restOfLine
        from _____ in CommonParsers.LineEnd
        select (key.ToLowerInvariant(), value.Trim());

    static Parser<char, Requirement> requirementBlockParser =
        from _ in CommonParsers.InlineWhitespace
        from type in requirementTypeParser
        from __ in CommonParsers.RequiredWhitespace
        from name in identifier
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char('{')
        from _____ in CommonParsers.LineEnd
        from props in propertyParser.Many()
        from ______ in CommonParsers.InlineWhitespace
        from _______ in Char('}')
        from ________ in CommonParsers.InlineWhitespace
        from _________ in CommonParsers.LineEnd
        select BuildRequirement(name, type, props.ToList());

    static Requirement BuildRequirement(string name, RequirementType type, List<(string key, string value)> props)
    {
        var req = new Requirement { Id = name, Name = name, Type = type };
        foreach (var (key, value) in props)
        {
            switch (key)
            {
                case "id": req.Id = value; break;
                case "text": req.Text = value; break;
                case "risk":
                    req.Risk = value.ToLowerInvariant() switch
                    {
                        "low" => RiskLevel.Low,
                        "high" => RiskLevel.High,
                        _ => RiskLevel.Medium
                    };
                    break;
                case "verifymethod":
                    req.VerifyMethod = value.ToLowerInvariant() switch
                    {
                        "analysis" => VerifyMethod.Analysis,
                        "demonstration" => VerifyMethod.Demonstration,
                        "inspection" => VerifyMethod.Inspection,
                        _ => VerifyMethod.Test
                    };
                    break;
            }
        }
        return req;
    }

    static Parser<char, RequirementElement> elementBlockParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("element")
        from ___ in CommonParsers.RequiredWhitespace
        from name in identifier
        from ____ in CommonParsers.InlineWhitespace
        from _____ in Char('{')
        from ______ in CommonParsers.LineEnd
        from props in propertyParser.Many()
        from _______ in CommonParsers.InlineWhitespace
        from ________ in Char('}')
        from _________ in CommonParsers.InlineWhitespace
        from __________ in CommonParsers.LineEnd
        select BuildElement(name, props.ToList());

    static RequirementElement BuildElement(string name, List<(string key, string value)> props)
    {
        var elem = new RequirementElement { Id = name, Name = name };
        foreach (var (key, value) in props)
        {
            switch (key)
            {
                case "type": elem.Type = value; break;
                case "docref": elem.DocRef = value; break;
            }
        }
        return elem;
    }

    static Parser<char, RelationType> relationTypeParser =
        OneOf(
            Try(CIString("contains")).ThenReturn(RelationType.Contains),
            Try(CIString("copies")).ThenReturn(RelationType.Copies),
            Try(CIString("derives")).ThenReturn(RelationType.Derives),
            Try(CIString("satisfies")).ThenReturn(RelationType.Satisfies),
            Try(CIString("verifies")).ThenReturn(RelationType.Verifies),
            Try(CIString("refines")).ThenReturn(RelationType.Refines),
            CIString("traces").ThenReturn(RelationType.Traces)
        );

    // Relation: source - type -> target
    static Parser<char, RequirementRelation> relationParser =
        from _ in CommonParsers.InlineWhitespace
        from source in identifier
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char('-')
        from ____ in CommonParsers.InlineWhitespace
        from relType in relationTypeParser
        from _____ in CommonParsers.InlineWhitespace
        from ______ in String("->")
        from _______ in CommonParsers.InlineWhitespace
        from target in identifier
        from ________ in CommonParsers.InlineWhitespace
        from _________ in CommonParsers.LineEnd
        select new RequirementRelation
        {
            Source = source,
            Target = target,
            Type = relType
        };

    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    static Parser<char, IRequirementContent?> ContentItem =>
        OneOf(
            Try(requirementBlockParser.Select<IRequirementContent?>(_ => new RequirementBlockItem(_))),
            Try(elementBlockParser.Select<IRequirementContent?>(_ => new ElementBlockItem(_))),
            Try(relationParser.Select<IRequirementContent?>(_ => new RelationItem(_))),
            skipLine.ThenReturn<IRequirementContent?>(null)
        );

    public static Parser<char, RequirementModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("requirementDiagram")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1);

    static RequirementModel BuildModel(IEnumerable<IRequirementContent?> content)
    {
        var model = new RequirementModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case RequirementBlockItem req:
                    model.Requirements.Add(req.Value);
                    break;

                case ElementBlockItem elem:
                    model.Elements.Add(elem.Value);
                    break;

                case RelationItem rel:
                    model.Relations.Add(rel.Value);
                    break;
            }
        }

        return model;
    }

    public Result<char, RequirementModel> Parse(string input) => Parser.Parse(input);

    internal interface IRequirementContent;
    readonly record struct RequirementBlockItem(Requirement Value) : IRequirementContent;
    readonly record struct ElementBlockItem(RequirementElement Value) : IRequirementContent;
    readonly record struct RelationItem(RequirementRelation Value) : IRequirementContent;
}

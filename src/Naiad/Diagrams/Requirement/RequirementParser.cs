namespace MermaidSharp.Diagrams.Requirement;

public class RequirementParser : IDiagramParser<RequirementModel>
{
    public DiagramType DiagramType => DiagramType.Requirement;

    // Identifier
    static Parser<char, string> Identifier =
        Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').AtLeastOnceString();

    // Rest of line
    static Parser<char, string> RestOfLine =
        Token(c => c != '\r' && c != '\n').ManyString();

    // Requirement type
    static Parser<char, RequirementType> RequirementTypeParser =
        OneOf(
            Try(CIString("functionalRequirement")).ThenReturn(RequirementType.FunctionalRequirement),
            Try(CIString("interfaceRequirement")).ThenReturn(RequirementType.InterfaceRequirement),
            Try(CIString("performanceRequirement")).ThenReturn(RequirementType.PerformanceRequirement),
            Try(CIString("physicalRequirement")).ThenReturn(RequirementType.PhysicalRequirement),
            Try(CIString("designConstraint")).ThenReturn(RequirementType.DesignConstraint),
            CIString("requirement").ThenReturn(RequirementType.Requirement)
        );

    // Property: key: value
    static Parser<char, (string key, string value)> PropertyParser =
        from _ in CommonParsers.InlineWhitespace
        from key in Identifier
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from value in RestOfLine
        from _____ in CommonParsers.LineEnd
        select (key.ToLowerInvariant(), value.Trim());

    // Requirement block
    static Parser<char, Requirement> RequirementBlockParser =
        from _ in CommonParsers.InlineWhitespace
        from type in RequirementTypeParser
        from __ in CommonParsers.RequiredWhitespace
        from name in Identifier
        from ___ in CommonParsers.InlineWhitespace
        from ____ in Char('{')
        from _____ in CommonParsers.LineEnd
        from props in PropertyParser.Many()
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

    // Element block
    static Parser<char, RequirementElement> ElementBlockParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("element")
        from ___ in CommonParsers.RequiredWhitespace
        from name in Identifier
        from ____ in CommonParsers.InlineWhitespace
        from _____ in Char('{')
        from ______ in CommonParsers.LineEnd
        from props in PropertyParser.Many()
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

    // Relation type
    static Parser<char, RelationType> RelationTypeParser =
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
    static Parser<char, RequirementRelation> RelationParser =
        from _ in CommonParsers.InlineWhitespace
        from source in Identifier
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char('-')
        from ____ in CommonParsers.InlineWhitespace
        from relType in RelationTypeParser
        from _____ in CommonParsers.InlineWhitespace
        from ______ in String("->")
        from _______ in CommonParsers.InlineWhitespace
        from target in Identifier
        from ________ in CommonParsers.InlineWhitespace
        from _________ in CommonParsers.LineEnd
        select new RequirementRelation
        {
            Source = source,
            Target = target,
            Type = relType
        };

    // Skip line
    static Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(RequirementBlockParser.Select(r => (object?)("requirement", r))),
            Try(ElementBlockParser.Select(e => (object?)("element", e))),
            Try(RelationParser.Select(r => (object?)("relation", r))),
            SkipLine.ThenReturn((object?)null)
        );

    public static Parser<char, RequirementModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("requirementDiagram")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

    static RequirementModel BuildModel(List<object?> content)
    {
        var model = new RequirementModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case ("requirement", Requirement req):
                    model.Requirements.Add(req);
                    break;

                case ("element", RequirementElement elem):
                    model.Elements.Add(elem);
                    break;

                case ("relation", RequirementRelation rel):
                    model.Relations.Add(rel);
                    break;
            }
        }

        return model;
    }

    public Result<char, RequirementModel> Parse(string input) => Parser.Parse(input);
}

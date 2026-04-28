class C4Parser : IDiagramParser<C4Model>
{
    public DiagramType DiagramType => DiagramType.C4Context;

    static Parser<char, string> identifier =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

    static Parser<char, string> quotedString =
        Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

    static Parser<char, string> restOfLine =
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    // Title: title My Diagram
    static Parser<char, string> titleParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("title")
        from ___ in CommonParsers.RequiredWhitespace
        from title in restOfLine
        from ____ in CommonParsers.LineEnd
        select title.Trim();

    // Person(id, "label", "description")
    static Parser<char, C4Element> personParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(Try(CIString("Person_Ext")), CIString("Person"))
        from __ in Char('(')
        from id in identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from desc in Try(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
            .Then(quotedString)
        ).Optional()
        from ____ in Char(')')
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new C4Element
        {
            Id = id,
            Label = label,
            Description = desc.GetValueOrDefault(),
            Type = C4ElementType.Person,
            IsExternal = type.Contains("Ext", StringComparison.OrdinalIgnoreCase)
        };

    // System(id, "label", "description") or System_Ext
    static Parser<char, C4Element> systemParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(Try(CIString("System_Ext")), CIString("System"))
        from __ in Char('(')
        from id in identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from desc in Try(
            CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
            .Then(quotedString)
        ).Optional()
        from ____ in Char(')')
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new C4Element
        {
            Id = id,
            Label = label,
            Description = desc.GetValueOrDefault(),
            Type = C4ElementType.System,
            IsExternal = type.Contains("Ext", StringComparison.OrdinalIgnoreCase)
        };

    // Optional quoted string with comma prefix
    static Parser<char, string> optionalQuotedArg =
        CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
            .Then(quotedString);

    // SystemDb(id, "label", "description") or SystemDb_Ext
    static Parser<char, C4Element> systemDbParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(Try(CIString("SystemDb_Ext")), CIString("SystemDb"))
        from __ in Char('(')
        from id in identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from desc in Try(optionalQuotedArg).Optional()
        from ____ in Char(')')
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new C4Element
        {
            Id = id,
            Label = label,
            Description = desc.GetValueOrDefault(),
            Type = C4ElementType.SystemDb,
            IsExternal = type.Contains("Ext", StringComparison.OrdinalIgnoreCase)
        };

    // Container(id, "label", "tech", "description") or Container_Ext
    static Parser<char, C4Element> containerParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(
            Try(CIString("Container_Ext")),
            Try(CIString("ContainerDb_Ext")),
            Try(CIString("ContainerQueue_Ext")),
            Try(CIString("ContainerDb")),
            Try(CIString("ContainerQueue")),
            CIString("Container"))
        from __ in Char('(')
        from id in identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from tech in Try(optionalQuotedArg).Optional()
        from desc in Try(optionalQuotedArg).Optional()
        from ____ in Char(')')
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new C4Element
        {
            Id = id,
            Label = label,
            Technology = tech.GetValueOrDefault(),
            Description = desc.GetValueOrDefault(),
            Type = type.Contains("Db", StringComparison.OrdinalIgnoreCase) ? C4ElementType.ContainerDb :
                   type.Contains("Queue", StringComparison.OrdinalIgnoreCase) ? C4ElementType.ContainerQueue :
                   C4ElementType.Container,
            IsExternal = type.Contains("Ext", StringComparison.OrdinalIgnoreCase)
        };

    // Component(id, "label", "tech", "description")
    static Parser<char, C4Element> componentParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(Try(CIString("Component_Ext")), CIString("Component"))
        from __ in Char('(')
        from id in identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from tech in Try(optionalQuotedArg).Optional()
        from desc in Try(optionalQuotedArg).Optional()
        from ____ in Char(')')
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new C4Element
        {
            Id = id,
            Label = label,
            Technology = tech.GetValueOrDefault(),
            Description = desc.GetValueOrDefault(),
            Type = C4ElementType.Component,
            IsExternal = type.Contains("Ext", StringComparison.OrdinalIgnoreCase)
        };

    // Rel(from, to, "label", "tech")
    static Parser<char, C4Relationship> relParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in OneOf(
            Try(CIString("Rel_D")), Try(CIString("Rel_U")),
            Try(CIString("Rel_L")), Try(CIString("Rel_R")),
            Try(CIString("Rel_Back")), Try(CIString("Rel_Neighbor")),
            CIString("Rel"))
        from ___ in Char('(')
        from fromId in identifier
        from ____ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from toId in identifier
        from label in Try(optionalQuotedArg).Optional()
        from tech in Try(optionalQuotedArg).Optional()
        from _____ in Char(')')
        from ______ in CommonParsers.InlineWhitespace
        from _______ in CommonParsers.LineEnd
        select new C4Relationship
        {
            From = fromId,
            To = toId,
            Label = label.GetValueOrDefault(),
            Technology = tech.GetValueOrDefault()
        };

    // Skip line (comments, empty lines)
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Boundary opening: Type_Boundary(id, "label") {
    public static Parser<char, (string id, string label, C4BoundaryType type)> BoundaryOpen =>
        from _ in CommonParsers.InlineWhitespace
        from boundaryType in OneOf(
            Try(CIString("Container_Boundary")).ThenReturn(C4BoundaryType.Container),
            Try(CIString("System_Boundary")).ThenReturn(C4BoundaryType.System),
            Try(CIString("Enterprise_Boundary")).ThenReturn(C4BoundaryType.Enterprise),
            Try(CIString("Deployment_Node")).ThenReturn(C4BoundaryType.Deployment),
            Try(CIString("Node_L")).ThenReturn(C4BoundaryType.Node),
            Try(CIString("Node_R")).ThenReturn(C4BoundaryType.Node),
            CIString("Node").ThenReturn(C4BoundaryType.Node))
        from __ in Char('(')
        from id in identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from desc in Try(optionalQuotedArg).Optional() // Optional description for nodes
        from ____ in Char(')')
        from _____ in CommonParsers.InlineWhitespace
        from ______ in Char('{')
        from _______ in CommonParsers.InlineWhitespace
        from ________ in CommonParsers.LineEnd.Optional()
        select (id, label, boundaryType);

    // Boundary closing: }
    public static Parser<char, Unit> boundaryClose =
        from _ in CommonParsers.InlineWhitespace
        from __ in Char('}')
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd.Optional()
        select Unit.Value;

    // Element inside boundary (sets BoundaryId later)
    public static Parser<char, IC4Content?> BoundaryContentItem =>
        OneOf(
            Try(personParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(systemDbParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(systemParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(containerParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(componentParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(relParser.Select<IC4Content?>(_ => new RelItem(_))),
            skipLine.ThenReturn<IC4Content?>(null)
        );

    // Recursive boundary parser - parses boundary with nested content
    public static Parser<char, BoundaryItem> BoundaryParser =>
        from open in BoundaryOpen
        from content in BoundaryContentOrNestedBoundary.Until(Lookahead(Try(boundaryClose)))
        from close in boundaryClose
        select new BoundaryItem(
            new C4Boundary { Id = open.id, Label = open.label, Type = open.type },
            content.ToList()
        );

    // Content inside boundary: either nested boundary or regular element
    public static Parser<char, IC4Content?> BoundaryContentOrNestedBoundary =>
        OneOf(
            Try(BoundaryParser.Select<IC4Content?>(_ => _)),
            BoundaryContentItem
        );

    // Content item (top level)
    public static Parser<char, IC4Content?> ContentItem =>
        OneOf(
            Try(titleParser.Select<IC4Content?>(_ => new TitleItem(_))),
            Try(BoundaryParser.Select<IC4Content?>(_ => _)),
            Try(personParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(systemDbParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(systemParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(containerParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(componentParser.Select<IC4Content?>(_ => new ElementItem(_))),
            Try(relParser.Select<IC4Content?>(_ => new RelItem(_))),
            skipLine.ThenReturn<IC4Content?>(null)
        );

    // Diagram type header
    public static Parser<char, C4DiagramType> diagramTypeParser =
        OneOf(
            Try(CIString("C4Context")).ThenReturn(C4DiagramType.Context),
            Try(CIString("C4Container")).ThenReturn(C4DiagramType.Container),
            Try(CIString("C4Component")).ThenReturn(C4DiagramType.Component),
            Try(CIString("C4Deployment")).ThenReturn(C4DiagramType.Deployment)
        );

    public static Parser<char, C4Model> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from type in diagramTypeParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(type, result.Item1);

    static C4Model BuildModel(C4DiagramType type, IEnumerable<IC4Content?> content)
    {
        var model = new C4Model { Type = type };
        ProcessContent(model, content, null);
        return model;
    }

    static void ProcessContent(C4Model model, IEnumerable<IC4Content?> content, string? parentBoundaryId)
    {
        foreach (var item in content)
        {
            switch (item)
            {
                case TitleItem title:
                    model.Title = title.Value;
                    break;

                case ElementItem element:
                    element.Element.BoundaryId = parentBoundaryId;
                    model.Elements.Add(element.Element);
                    break;

                case RelItem rel:
                    model.Relationships.Add(rel.Value);
                    break;

                case BoundaryItem boundaryItem:
                    var boundary = boundaryItem.Boundary;
                    boundary.ElementIds.Clear();
                    boundary.ChildBoundaryIds.Clear();
                    boundary.ParentBoundaryId = parentBoundaryId;
                    model.Boundaries.Add(boundary);

                    // Add this boundary as child of parent
                    if (parentBoundaryId is not null)
                    {
                        var parent = model.Boundaries.FirstOrDefault(_ => _.Id == parentBoundaryId);
                        parent?.ChildBoundaryIds.Add(boundary.Id);
                    }

                    // Process nested content with this boundary as parent
                    ProcessContent(model, boundaryItem.Content, boundary.Id);

                    // Collect direct element IDs that belong to this boundary (not nested)
                    foreach (var el in model.Elements.Where(_ => _.BoundaryId == boundary.Id))
                    {
                        boundary.ElementIds.Add(el.Id);
                    }
                    break;
            }
        }
    }

    public Result<char, C4Model> Parse(string input) => Parser.Parse(input);

    internal interface IC4Content;
    internal readonly record struct TitleItem(string Value) : IC4Content;
    internal readonly record struct ElementItem(C4Element Element) : IC4Content;
    internal readonly record struct RelItem(C4Relationship Value) : IC4Content;

    internal readonly record struct BoundaryItem(C4Boundary Boundary, List<IC4Content?> Content) : IC4Content;
}

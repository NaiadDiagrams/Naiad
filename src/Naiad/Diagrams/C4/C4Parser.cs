namespace MermaidSharp.Diagrams.C4;

public class C4Parser : IDiagramParser<C4Model>
{
    public DiagramType DiagramType => DiagramType.C4Context;

    // Identifier
    static Parser<char, string> Identifier =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

    // Quoted string
    static Parser<char, string> quotedString =
        Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

    // Rest of line
    static Parser<char, string> restOfLine =
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    // Title: title My Diagram
    static Parser<char, string> ritleParser =
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
        from id in Identifier
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
    static Parser<char, C4Element> SystemParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(Try(CIString("System_Ext")), CIString("System"))
        from __ in Char('(')
        from id in Identifier
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
    static Parser<char, string> OptionalQuotedArg =
        CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
            .Then(quotedString);

    // SystemDb(id, "label", "description") or SystemDb_Ext
    static Parser<char, C4Element> SystemDbParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(Try(CIString("SystemDb_Ext")), CIString("SystemDb"))
        from __ in Char('(')
        from id in Identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from desc in Try(OptionalQuotedArg).Optional()
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
    static Parser<char, C4Element> ContainerParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(
            Try(CIString("Container_Ext")),
            Try(CIString("ContainerDb_Ext")),
            Try(CIString("ContainerQueue_Ext")),
            Try(CIString("ContainerDb")),
            Try(CIString("ContainerQueue")),
            CIString("Container"))
        from __ in Char('(')
        from id in Identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from tech in Try(OptionalQuotedArg).Optional()
        from desc in Try(OptionalQuotedArg).Optional()
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
    static Parser<char, C4Element> ComponentParser =
        from _ in CommonParsers.InlineWhitespace
        from type in OneOf(Try(CIString("Component_Ext")), CIString("Component"))
        from __ in Char('(')
        from id in Identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from tech in Try(OptionalQuotedArg).Optional()
        from desc in Try(OptionalQuotedArg).Optional()
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
        from fromId in Identifier
        from ____ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from toId in Identifier
        from label in Try(OptionalQuotedArg).Optional()
        from tech in Try(OptionalQuotedArg).Optional()
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
    static Parser<char, (string id, string label, C4BoundaryType type)> BoundaryOpen =>
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
        from id in Identifier
        from ___ in CommonParsers.InlineWhitespace.Then(Char(',')).Then(CommonParsers.InlineWhitespace)
        from label in quotedString
        from desc in Try(OptionalQuotedArg).Optional() // Optional description for nodes
        from ____ in Char(')')
        from _____ in CommonParsers.InlineWhitespace
        from ______ in Char('{')
        from _______ in CommonParsers.InlineWhitespace
        from ________ in CommonParsers.LineEnd.Optional()
        select (id, label, boundaryType);

    // Boundary closing: }
    static Parser<char, Unit> BoundaryClose =
        from _ in CommonParsers.InlineWhitespace
        from __ in Char('}')
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd.Optional()
        select Unit.Value;

    // Element inside boundary (sets BoundaryId later)
    static Parser<char, object?> BoundaryContentItem =>
        OneOf(
            Try(personParser.Select(e => (object?)("element", e))),
            Try(SystemDbParser.Select(e => (object?)("element", e))),
            Try(SystemParser.Select(e => (object?)("element", e))),
            Try(ContainerParser.Select(e => (object?)("element", e))),
            Try(ComponentParser.Select(e => (object?)("element", e))),
            Try(relParser.Select(r => (object?)("rel", r))),
            skipLine.ThenReturn((object?)null)
        );

    // Recursive boundary parser - parses boundary with nested content
    static Parser<char, (C4Boundary boundary, List<object?> content)> BoundaryParser =>
        from open in BoundaryOpen
        from content in BoundaryContentOrNestedBoundary.Until(Lookahead(Try(BoundaryClose)))
        from close in BoundaryClose
        select (
            new C4Boundary { Id = open.id, Label = open.label, Type = open.type },
            content.ToList()
        );

    // Content inside boundary: either nested boundary or regular element
    static Parser<char, object?> BoundaryContentOrNestedBoundary =>
        OneOf(
            Try(BoundaryParser.Select(b => (object?)("boundary", b))),
            BoundaryContentItem
        );

    // Content item (top level)
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(ritleParser.Select(t => (object?)("title", t))),
            Try(BoundaryParser.Select(b => (object?)("boundary", b))),
            Try(personParser.Select(e => (object?)("element", e))),
            Try(SystemDbParser.Select(e => (object?)("element", e))),
            Try(SystemParser.Select(e => (object?)("element", e))),
            Try(ContainerParser.Select(e => (object?)("element", e))),
            Try(ComponentParser.Select(e => (object?)("element", e))),
            Try(relParser.Select(r => (object?)("rel", r))),
            skipLine.ThenReturn((object?)null)
        );

    // Diagram type header
    static Parser<char, C4DiagramType> DiagramTypeParser =
        OneOf(
            Try(CIString("C4Context")).ThenReturn(C4DiagramType.Context),
            Try(CIString("C4Container")).ThenReturn(C4DiagramType.Container),
            Try(CIString("C4Component")).ThenReturn(C4DiagramType.Component),
            Try(CIString("C4Deployment")).ThenReturn(C4DiagramType.Deployment)
        );

    public static Parser<char, C4Model> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from type in DiagramTypeParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(type, result.Item1.Where(_ => _ != null).ToList());

    static C4Model BuildModel(C4DiagramType type, List<object?> content)
    {
        var model = new C4Model { Type = type };
        ProcessContent(model, content, null);
        return model;
    }

    static void ProcessContent(C4Model model, List<object?> content, string? parentBoundaryId)
    {
        foreach (var item in content)
        {
            switch (item)
            {
                case ("title", string title):
                    model.Title = title;
                    break;

                case ("element", C4Element element):
                    element.BoundaryId = parentBoundaryId;
                    model.Elements.Add(element);
                    break;

                case ("rel", C4Relationship rel):
                    model.Relationships.Add(rel);
                    break;

                case ("boundary", (C4Boundary boundary, List<object?> boundaryContent)):
                    boundary.ElementIds.Clear();
                    boundary.ChildBoundaryIds.Clear();
                    boundary.ParentBoundaryId = parentBoundaryId;
                    model.Boundaries.Add(boundary);

                    // Add this boundary as child of parent
                    if (parentBoundaryId is not null)
                    {
                        var parent = model.Boundaries.FirstOrDefault(b => b.Id == parentBoundaryId);
                        parent?.ChildBoundaryIds.Add(boundary.Id);
                    }

                    // Process nested content with this boundary as parent
                    ProcessContent(model, boundaryContent, boundary.Id);

                    // Collect direct element IDs that belong to this boundary (not nested)
                    foreach (var el in model.Elements.Where(e => e.BoundaryId == boundary.Id))
                    {
                        boundary.ElementIds.Add(el.Id);
                    }
                    break;
            }
        }
    }

    public Result<char, C4Model> Parse(string input) => Parser.Parse(input);
}

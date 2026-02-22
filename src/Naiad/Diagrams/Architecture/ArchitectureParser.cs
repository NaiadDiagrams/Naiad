namespace MermaidSharp.Diagrams.Architecture;

public class ArchitectureParser : IDiagramParser<ArchitectureModel>
{
    public DiagramType DiagramType => DiagramType.Architecture;

    static Parser<char, string> identifier =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

    static Parser<char, string> iconParser =
        Char('(').Then(Token(_ => _ != ')').ManyString()).Before(Char(')'));

    static Parser<char, string> labelParser =
        Char('[').Then(Token(_ => _ != ']').ManyString()).Before(Char(']'));

    static Parser<char, string> parentParser =
        Try(
            CommonParsers.RequiredWhitespace
                .Then(CIString("in"))
                .Then(CommonParsers.RequiredWhitespace)
                .Then(identifier)
        );

    // Direction: L, R, T, B
    static Parser<char, EdgeDirection> directionParser =
        OneOf(
            Char('L').ThenReturn(EdgeDirection.Left),
            Char('R').ThenReturn(EdgeDirection.Right),
            Char('T').ThenReturn(EdgeDirection.Top),
            Char('B').ThenReturn(EdgeDirection.Bottom)
        );

    // Group: group id(icon)[label] in parent
    static Parser<char, ArchitectureGroup> groupParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("group")
        from ___ in CommonParsers.RequiredWhitespace
        from id in identifier
        from icon in iconParser.Optional()
        from label in labelParser.Optional()
        from parent in parentParser.Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ArchitectureGroup
        {
            Id = id,
            Icon = icon.GetValueOrDefault(),
            Label = label.GetValueOrDefault(),
            Parent = parent.GetValueOrDefault()
        };

    // Service: service id(icon)[label] in parent
    static Parser<char, ArchitectureService> serviceParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("service")
        from ___ in CommonParsers.RequiredWhitespace
        from id in identifier
        from icon in iconParser.Optional()
        from label in labelParser.Optional()
        from parent in parentParser.Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ArchitectureService
        {
            Id = id,
            Icon = icon.GetValueOrDefault(),
            Label = label.GetValueOrDefault(),
            Parent = parent.GetValueOrDefault()
        };

    // Junction: junction id in parent
    static Parser<char, ArchitectureJunction> junctionParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("junction")
        from ___ in CommonParsers.RequiredWhitespace
        from id in identifier
        from parent in parentParser.Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ArchitectureJunction
        {
            Id = id,
            Parent = parent.GetValueOrDefault()
        };

    // Group reference: {groupId}
    static Parser<char, string> groupRef =
        Char('{').Then(identifier).Before(Char('}'));

    // Source side: id{group}?:direction with optional arrow
    static Parser<char, (string id, string? grp, EdgeDirection dir, bool arrow)> sourceSideParser =
        from arw in Char('<').Optional()
        from nodeId in identifier
        from grp in groupRef.Optional()
        from colon in Char(':')
        from dir in directionParser
        select (nodeId, grp.GetValueOrDefault(), dir, arw.HasValue);

    // Target side: direction:id{group}? with optional arrow
    static Parser<char, (string id, string? grp, EdgeDirection dir, bool arrow)> targetSideParser =
        from dir in directionParser
        from arw in Char('>').Optional()
        from colon in Char(':')
        from nodeId in identifier
        from grp in groupRef.Optional()
        select (nodeId, grp.GetValueOrDefault(), dir, arw.HasValue);

    // Edge: source:side <arrow>--<arrow> side:target
    static Parser<char, ArchitectureEdge> edgeParser =
        from _ in CommonParsers.InlineWhitespace
        from source in sourceSideParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in String("--")
        from ____ in CommonParsers.InlineWhitespace
        from target in targetSideParser
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select BuildEdge(source, target);

    static ArchitectureEdge BuildEdge(
        (string id, string? grp, EdgeDirection dir, bool arrow) source,
        (string id, string? grp, EdgeDirection dir, bool arrow) target) => new()
    {
        SourceId = source.id,
        SourceGroup = source.grp,
        SourceSide = source.dir,
        SourceArrow = source.arrow,
        TargetId = target.id,
        TargetGroup = target.grp,
        TargetSide = target.dir,
        TargetArrow = target.arrow
    };

    // Skip line
    static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(groupParser.Select(g => (object?)(ItemType.Group, g))),
            Try(serviceParser.Select(s => (object?)(ItemType.Service, s))),
            Try(junctionParser.Select(j => (object?)(ItemType.Junction, j))),
            Try(edgeParser.Select(e => (object?)(ItemType.Edge, e))),
            skipLine.ThenReturn((object?)null)
        );

    enum ItemType { Group, Service, Junction, Edge }

    public static Parser<char, ArchitectureModel> Parser =>
        from inlineWhitespace in CommonParsers.InlineWhitespace
        from architecture in CIString("architecture-beta")
        from innerInlineWhitespace in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(_ => _ != null).ToList());

    static ArchitectureModel BuildModel(List<object?> content)
    {
        var model = new ArchitectureModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case (ItemType.Group, ArchitectureGroup group):
                    model.Groups.Add(group);
                    break;

                case (ItemType.Service, ArchitectureService service):
                    model.Services.Add(service);
                    break;

                case (ItemType.Junction, ArchitectureJunction junction):
                    model.Junctions.Add(junction);
                    break;

                case (ItemType.Edge, ArchitectureEdge edge):
                    model.Edges.Add(edge);
                    break;
            }
        }

        return model;
    }

    public Result<char, ArchitectureModel> Parse(string input) => Parser.Parse(input);
}

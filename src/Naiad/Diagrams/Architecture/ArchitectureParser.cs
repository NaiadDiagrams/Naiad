namespace MermaidSharp.Diagrams.Architecture;

public class ArchitectureParser : IDiagramParser<ArchitectureModel>
{
    public DiagramType DiagramType => DiagramType.Architecture;

    // Identifier
    static readonly Parser<char, string> Identifier =
        Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').AtLeastOnceString();

    // Icon: (iconName)
    static readonly Parser<char, string> IconParser =
        Char('(').Then(Token(c => c != ')').ManyString()).Before(Char(')'));

    // Label: [text]
    static readonly Parser<char, string> LabelParser =
        Char('[').Then(Token(c => c != ']').ManyString()).Before(Char(']'));

    // Parent: in parentId
    static readonly Parser<char, string> ParentParser =
        Try(
            CommonParsers.RequiredWhitespace
                .Then(CIString("in"))
                .Then(CommonParsers.RequiredWhitespace)
                .Then(Identifier)
        );

    // Direction: L, R, T, B
    static readonly Parser<char, EdgeDirection> DirectionParser =
        OneOf(
            Char('L').ThenReturn(EdgeDirection.Left),
            Char('R').ThenReturn(EdgeDirection.Right),
            Char('T').ThenReturn(EdgeDirection.Top),
            Char('B').ThenReturn(EdgeDirection.Bottom)
        );

    // Group: group id(icon)[label] in parent
    static readonly Parser<char, ArchitectureGroup> GroupParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("group")
        from ___ in CommonParsers.RequiredWhitespace
        from id in Identifier
        from icon in IconParser.Optional()
        from label in LabelParser.Optional()
        from parent in ParentParser.Optional()
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
    static readonly Parser<char, ArchitectureService> ServiceParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("service")
        from ___ in CommonParsers.RequiredWhitespace
        from id in Identifier
        from icon in IconParser.Optional()
        from label in LabelParser.Optional()
        from parent in ParentParser.Optional()
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
    static readonly Parser<char, ArchitectureJunction> JunctionParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("junction")
        from ___ in CommonParsers.RequiredWhitespace
        from id in Identifier
        from parent in ParentParser.Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new ArchitectureJunction
        {
            Id = id,
            Parent = parent.GetValueOrDefault()
        };

    // Group reference: {groupId}
    static readonly Parser<char, string> GroupRef =
        Char('{').Then(Identifier).Before(Char('}'));

    // Source side: id{group}?:direction with optional arrow
    static readonly Parser<char, (string id, string? grp, EdgeDirection dir, bool arrow)> SourceSideParser =
        from arw in Char('<').Optional()
        from nodeId in Identifier
        from grp in GroupRef.Optional()
        from _ in Char(':')
        from dir in DirectionParser
        select (nodeId, grp.GetValueOrDefault(), dir, arw.HasValue);

    // Target side: direction:id{group}? with optional arrow
    static readonly Parser<char, (string id, string? grp, EdgeDirection dir, bool arrow)> TargetSideParser =
        from dir in DirectionParser
        from arw in Char('>').Optional()
        from _ in Char(':')
        from nodeId in Identifier
        from grp in GroupRef.Optional()
        select (nodeId, grp.GetValueOrDefault(), dir, arw.HasValue);

    // Edge: source:side <arrow>--<arrow> side:target
    static readonly Parser<char, ArchitectureEdge> EdgeParser =
        from _ in CommonParsers.InlineWhitespace
        from source in SourceSideParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in String("--")
        from ____ in CommonParsers.InlineWhitespace
        from target in TargetSideParser
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
    static readonly Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, object?> ContentItem =>
        OneOf(
            Try(GroupParser.Select(g => (object?)(ItemType.Group, g))),
            Try(ServiceParser.Select(s => (object?)(ItemType.Service, s))),
            Try(JunctionParser.Select(j => (object?)(ItemType.Junction, j))),
            Try(EdgeParser.Select(e => (object?)(ItemType.Edge, e))),
            SkipLine.ThenReturn((object?)null)
        );

    enum ItemType { Group, Service, Junction, Edge }

    public static Parser<char, ArchitectureModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("architecture-beta")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(c => c != null).ToList());

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

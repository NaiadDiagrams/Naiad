class ArchitectureParser : IDiagramParser<ArchitectureModel>
{
    static readonly Parser<char, string> identifier;

    static readonly Parser<char, string> iconParser;

    static readonly Parser<char, string> labelParser;

    static readonly Parser<char, string> parentParser;

    // Direction: L, R, T, B
    static readonly Parser<char, EdgeDirection> directionParser;

    // Group: group id(icon)[label] in parent
    static readonly Parser<char, ArchitectureGroup> groupParser;

    // Service: service id(icon)[label] in parent
    static readonly Parser<char, ArchitectureService> serviceParser;

    // Junction: junction id in parent
    static readonly Parser<char, ArchitectureJunction> junctionParser;

    // Group reference: {groupId}
    static readonly Parser<char, string> groupRef;

    // Source side: id{group}?:direction with optional arrow
    static readonly Parser<char, (string id, string? grp, EdgeDirection dir, bool arrow)> sourceSideParser;

    // Target side: direction:id{group}? with optional arrow
    static readonly Parser<char, (string id, string? grp, EdgeDirection dir, bool arrow)> targetSideParser;

    // Edge: source:side <arrow>--<arrow> side:target
    static readonly Parser<char, ArchitectureEdge> edgeParser;

    static readonly Parser<char, Unit> skipLine;

    static readonly Parser<char, IArchitectureContent?> ContentItem;

    static readonly Parser<char, ArchitectureModel> Parser;

    static ArchitectureParser()
    {
        identifier =
            Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

        iconParser =
            Char('(').Then(Token(_ => _ != ')').ManyString()).Before(Char(')'));

        labelParser =
            Char('[').Then(Token(_ => _ != ']').ManyString()).Before(Char(']'));

        parentParser =
            Try(
                CommonParsers.RequiredWhitespace
                    .Then(CIString("in"))
                    .Then(CommonParsers.RequiredWhitespace)
                    .Then(identifier)
            );

        directionParser =
            OneOf(
                Char('L').ThenReturn(EdgeDirection.Left),
                Char('R').ThenReturn(EdgeDirection.Right),
                Char('T').ThenReturn(EdgeDirection.Top),
                Char('B').ThenReturn(EdgeDirection.Bottom)
            );

        groupParser =
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

        serviceParser =
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

        junctionParser =
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

        groupRef =
            Char('{').Then(identifier).Before(Char('}'));

        sourceSideParser =
            from arw in Char('<').Optional()
            from nodeId in identifier
            from grp in groupRef.Optional()
            from colon in Char(':')
            from dir in directionParser
            select (nodeId, grp.GetValueOrDefault(), dir, arw.HasValue);

        targetSideParser =
            from dir in directionParser
            from arw in Char('>').Optional()
            from colon in Char(':')
            from nodeId in identifier
            from grp in groupRef.Optional()
            select (nodeId, grp.GetValueOrDefault(), dir, arw.HasValue);

        edgeParser =
            from _ in CommonParsers.InlineWhitespace
            from source in sourceSideParser
            from __ in CommonParsers.InlineWhitespace
            from ___ in String("--")
            from ____ in CommonParsers.InlineWhitespace
            from target in targetSideParser
            from _____ in CommonParsers.InlineWhitespace
            from ______ in CommonParsers.LineEnd
            select BuildEdge(source, target);

        skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        ContentItem =
            OneOf(
                Try(groupParser.Select<IArchitectureContent?>(_ => new GroupItem(_))),
                Try(serviceParser.Select<IArchitectureContent?>(_ => new ServiceItem(_))),
                Try(junctionParser.Select<IArchitectureContent?>(_ => new JunctionItem(_))),
                Try(edgeParser.Select<IArchitectureContent?>(_ => new EdgeItem(_))),
                skipLine.ThenReturn<IArchitectureContent?>(null)
            );

        Parser =
            from inlineWhitespace in CommonParsers.InlineWhitespace
            from architecture in CIString("architecture-beta")
            from innerInlineWhitespace in CommonParsers.InlineWhitespace
            from lineEnd in CommonParsers.LineEnd
            from result in ContentItem.ManyThen(End)
            select BuildModel(result.Item1);
    }

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

    static ArchitectureModel BuildModel(IEnumerable<IArchitectureContent?> content)
    {
        var model = new ArchitectureModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case GroupItem group:
                    model.Groups.Add(group.Value);
                    break;

                case ServiceItem service:
                    model.Services.Add(service.Value);
                    break;

                case JunctionItem junction:
                    model.Junctions.Add(junction.Value);
                    break;

                case EdgeItem edge:
                    model.Edges.Add(edge.Value);
                    break;
            }
        }

        return model;
    }

    public Result<char, ArchitectureModel> Parse(string input) => Parser.Parse(input);

    internal interface IArchitectureContent;
    readonly record struct GroupItem(ArchitectureGroup Value) : IArchitectureContent;
    readonly record struct ServiceItem(ArchitectureService Value) : IArchitectureContent;
    readonly record struct JunctionItem(ArchitectureJunction Value) : IArchitectureContent;
    readonly record struct EdgeItem(ArchitectureEdge Value) : IArchitectureContent;
}

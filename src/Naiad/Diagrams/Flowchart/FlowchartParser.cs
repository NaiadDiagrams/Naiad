class FlowchartParser : IDiagramParser<FlowchartModel>
{
    // Node shape parsers - returns (label, shape)
    static Parser<char, (string Label, NodeShape Shape)> doubleCircleShape =
        String("(((")
            .Then(Token(_ => _ != ')').ManyString())
            .Before(String(")))"))
            .Select(text => (text, NodeShape.DoubleCircle));

    static Parser<char, (string Label, NodeShape Shape)> circleShape =
        String("((")
            .Then(Token(_ => _ != ')').ManyString())
            .Before(String("))"))
            .Select(text => (text, NodeShape.Circle));

    static Parser<char, (string Label, NodeShape Shape)> stadiumShape =
        String("([")
            .Then(Token(_ => _ != ']').ManyString())
            .Before(String("])"))
            .Select(text => (text, NodeShape.Stadium));

    static Parser<char, (string Label, NodeShape Shape)> subroutineShape =
        String("[[")
            .Then(Token(_ => _ != ']').ManyString())
            .Before(String("]]"))
            .Select(text => (text, NodeShape.Subroutine));

    static Parser<char, (string Label, NodeShape Shape)> cylinderShape =
        String("[(")
            .Then(Token(_ => _ != ')').ManyString())
            .Before(String(")]"))
            .Select(text => (text, NodeShape.Cylinder));

    static Parser<char, (string Label, NodeShape Shape)> hexagonShape =
        String("{{")
            .Then(Token(_ => _ != '}').ManyString())
            .Before(String("}}"))
            .Select(text => (text, NodeShape.Hexagon));

    static Parser<char, (string Label, NodeShape Shape)> diamondShape =
        Char('{')
            .Then(Token(_ => _ != '}').ManyString())
            .Before(Char('}'))
            .Select(text => (text, NodeShape.Diamond));

    static Parser<char, (string Label, NodeShape Shape)> roundedShape =
        Char('(')
            .Then(Token(_ => _ != ')').ManyString())
            .Before(Char(')'))
            .Select(text => (text, NodeShape.RoundedRectangle));

    static Parser<char, (string Label, NodeShape Shape)> rectangleShape =
        Char('[')
            .Then(Token(_ => _ != ']').ManyString())
            .Before(Char(']'))
            .Select(text => (text, NodeShape.Rectangle));

    static Parser<char, (string Label, NodeShape Shape)> asymmetricShape =
        Char('>')
            .Then(Token(_ => _ != ']').ManyString())
            .Before(Char(']'))
            .Select(text => (text, NodeShape.Asymmetric));

    // Slash/backslash framed shapes. The text excludes / \ ] so the closing token is unambiguous; the
    // opener (`[/` vs `[\`) and closer (`/]` vs `\]`) together pick parallelogram vs trapezoid.
    static Parser<char, string> SlashShapeText =>
        Token(_ => _ != '/' && _ != '\\' && _ != ']').ManyString();

    static Parser<char, (string Label, NodeShape Shape)> parallelogramShape =
        String("[/").Then(SlashShapeText).Before(String("/]")).Select(text => (text, NodeShape.Parallelogram));

    static Parser<char, (string Label, NodeShape Shape)> trapezoidShape =
        String("[/").Then(SlashShapeText).Before(String("\\]")).Select(text => (text, NodeShape.Trapezoid));

    static Parser<char, (string Label, NodeShape Shape)> parallelogramAltShape =
        String("[\\").Then(SlashShapeText).Before(String("\\]")).Select(text => (text, NodeShape.ParallelogramAlt));

    static Parser<char, (string Label, NodeShape Shape)> trapezoidAltShape =
        String("[\\").Then(SlashShapeText).Before(String("/]")).Select(text => (text, NodeShape.TrapezoidAlt));

    static Parser<char, (string Label, NodeShape Shape)> nodeShapeParser =
        OneOf(
            Try(doubleCircleShape),
            Try(circleShape),
            Try(stadiumShape),
            Try(subroutineShape),
            Try(cylinderShape),
            Try(hexagonShape),
            Try(diamondShape),
            Try(roundedShape),
            Try(asymmetricShape),
            Try(parallelogramShape),
            Try(trapezoidShape),
            Try(parallelogramAltShape),
            Try(trapezoidAltShape),
            rectangleShape
        );

    // Node parser: identifier optionally followed by shape, then an optional `:::class` shorthand whose
    // class name is recorded on the node and resolved against `classDef` styles at model-build time.
    static Parser<char, Node> nodeParser =
        from id in CommonParsers.Identifier
        from shape in nodeShapeParser.Optional()
        from _class in Try(String(":::").Then(CommonParsers.Identifier)).Optional()
        select BuildNode(id, shape, _class);

    static Node BuildNode(string id, Maybe<(string Label, NodeShape Shape)> shape, Maybe<string> styleClass)
    {
        var node = new Node
        {
            Id = id,
            Label = shape.HasValue ? shape.Value.Label : null,
            Shape = shape.HasValue ? shape.Value.Shape : NodeShape.Rectangle
        };
        if (styleClass.HasValue)
        {
            node.Classes.Add(styleClass.Value);
        }

        return node;
    }

    static Parser<char, (EdgeType Type, EdgeStyle Style)> arrowTypeParser =
        OneOf(
            Try(String("<-->")).ThenReturn((EdgeType.BiDirectional, EdgeStyle.Solid)),
            Try(String("o--o")).ThenReturn((EdgeType.BiDirectionalCircle, EdgeStyle.Solid)),
            Try(String("x--x")).ThenReturn((EdgeType.BiDirectionalCross, EdgeStyle.Solid)),
            Try(String("-.->")).ThenReturn((EdgeType.DottedArrow, EdgeStyle.Dotted)),
            Try(String("-.-")).ThenReturn((EdgeType.Dotted, EdgeStyle.Dotted)),
            Try(String("==>")).ThenReturn((EdgeType.ThickArrow, EdgeStyle.Thick)),
            Try(String("===")).ThenReturn((EdgeType.Thick, EdgeStyle.Thick)),
            Try(String("--o")).ThenReturn((EdgeType.CircleEnd, EdgeStyle.Solid)),
            Try(String("--x")).ThenReturn((EdgeType.CrossEnd, EdgeStyle.Solid)),
            Try(String("-->")).ThenReturn((EdgeType.Arrow, EdgeStyle.Solid)),
            String("---").ThenReturn((EdgeType.Open, EdgeStyle.Solid))
        );

    // Inline edge label, e.g. `-- text -->`, `-. text .->`, `== text ==>`. The text runs up to the closing
    // token (which also fixes the arrow head); ends are ordered so a longer token (`.->`) is matched before
    // a token it contains (`.-`).
    static Parser<char, (EdgeType Type, EdgeStyle Style, string? Label)> InlineLabeled(
        string start,
        EdgeStyle style,
        (string End, EdgeType Type)[] ends) =>
        String(start)
            .Then(
                OneOf(
                    ends
                        .Select(end =>
                            Try(
                                Token(_ => _ != '\r' && _ != '\n')
                                    .Until(Lookahead(Try(String(end.End))))
                                    .Before(String(end.End))
                                    .Select(chars =>
                                    {
                                        var label = new string(chars.ToArray()).Trim();
                                        return (end.Type, style, label.Length == 0 ? (string?) null : label);
                                    })))
                        .ToArray()));

    static Parser<char, (EdgeType Type, EdgeStyle Style, string? Label)> inlineLabeledArrow =>
        OneOf(
            Try(InlineLabeled("--", EdgeStyle.Solid, [("-->", EdgeType.Arrow), ("--o", EdgeType.CircleEnd), ("--x", EdgeType.CrossEnd), ("---", EdgeType.Open)])),
            Try(InlineLabeled("-.", EdgeStyle.Dotted, [(".->", EdgeType.DottedArrow), (".-", EdgeType.Dotted)])),
            InlineLabeled("==", EdgeStyle.Thick, [("==>", EdgeType.ThickArrow), ("===", EdgeType.Thick)]));

    // A connector is a contiguous arrow (no label) or an inline-labeled arrow. Contiguous is tried first so
    // `-->`, `-.->`, `==>` etc. never fall through to the slower inline scan.
    static Parser<char, (EdgeType Type, EdgeStyle Style, string? Label)> connectorParser =>
        OneOf(
            Try(arrowTypeParser.Select(arrow => (arrow.Type, arrow.Style, (string?) null))),
            inlineLabeledArrow);

    // Edge label: |text|
    static Parser<char, string> edgeLabelParser =
        Char('|')
            .Then(Token(_ => _ != '|').ManyString())
            .Before(Char('|'));

    static Parser<char, Direction> flowchartDirection =
        OneOf(
            Try(String("TB")).ThenReturn(Direction.TopToBottom),
            Try(String("TD")).ThenReturn(Direction.TopToBottom),
            Try(String("BT")).ThenReturn(Direction.BottomToTop),
            Try(String("LR")).ThenReturn(Direction.LeftToRight),
            String("RL").ThenReturn(Direction.RightToLeft)
        );

    // Statement: A --> B --> C (chain of nodes with edges)
    public static Parser<char, (List<Node> Nodes, List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges)> StatementParser =>
        from first in nodeParser
        from rest in (
            from _1 in CommonParsers.InlineWhitespace
            from label1 in edgeLabelParser.Optional()
            from _2 in CommonParsers.InlineWhitespace
            from conn in connectorParser
            from _3 in CommonParsers.InlineWhitespace
            from label2 in edgeLabelParser.Optional()
            from _4 in CommonParsers.InlineWhitespace
            from node in nodeParser
            select (node, conn.Type, conn.Style, label1.HasValue ? label1.Value : conn.Label ?? (label2.HasValue ? label2.Value : null))
        ).Many()
        select (
            new List<Node>([first, .. rest.Select(_ => _.node)]),
            rest.Select(_ => (_.Type, _.Style, (string?) _.Item4)).ToList()
        );

    static Parser<char, string> NonWhitespaceToken =>
        Token(_ => _ != ' ' && _ != '\t' && _ != '\r' && _ != '\n').AtLeastOnceString();

    static Parser<char, string> RestOfLine =>
        Token(_ => _ != '\r' && _ != '\n').ManyString();

    // Style directive: `style NodeName fill:#color,stroke:#color` — applies an inline style to one element.
    static Parser<char, FlowStatement> styleDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("style")
        from ___ in CommonParsers.RequiredWhitespace
        from id in NonWhitespaceToken
        from ____ in CommonParsers.RequiredWhitespace
        from props in RestOfLine
        from lineEnd in CommonParsers.LineEnd
        select (FlowStatement) new StyleStatement(id, props);

    // Class definition: `classDef className fill:#color,...` (className may be a comma-separated list).
    static Parser<char, FlowStatement> classDefDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("classDef")
        from ___ in CommonParsers.RequiredWhitespace
        from names in NonWhitespaceToken
        from ____ in CommonParsers.RequiredWhitespace
        from props in RestOfLine
        from lineEnd in CommonParsers.LineEnd
        select (FlowStatement) new ClassDefStatement(names, props);

    // Class application: `class nodeId,nodeId2 className`.
    static Parser<char, FlowStatement> classDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("class")
        from ___ in CommonParsers.RequiredWhitespace
        from ids in NonWhitespaceToken
        from ____ in CommonParsers.RequiredWhitespace
        from className in NonWhitespaceToken
        from _____ in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        select (FlowStatement) new ClassAssignStatement(ids, className);

    // Click directive: click nodeId callback
    static Parser<char, Unit> clickDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("click")
        from ___ in CommonParsers.RequiredWhitespace
        from ____ in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Subgraph start: "subgraph id", "subgraph id[Label]" or "subgraph id [Label]"
    static Parser<char, FlowStatement> subgraphStart =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("subgraph")
        from __ in CommonParsers.RequiredWhitespace
        from id in CommonParsers.Identifier
        from label in (
            from _w in CommonParsers.InlineWhitespace
            from text in Char('[').Then(Token(_ => _ != ']').ManyString()).Before(Char(']'))
            select text
        ).Optional()
        from rest in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from lineEnd in CommonParsers.LineEnd
        select (FlowStatement) new SubgraphStartStatement(id, label.HasValue ? label.Value : null);

    // Subgraph end: end
    static Parser<char, FlowStatement> subgraphEnd =
        from _ in CommonParsers.InlineWhitespace
        from end in String("end")
        from ___ in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        select (FlowStatement) new SubgraphEndStatement();

    // Direction statement, e.g. `direction LR` (sets the enclosing subgraph's direction, or the chart's).
    static Parser<char, FlowStatement> directionStatement =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("direction")
        from __ in CommonParsers.RequiredWhitespace
        from dir in flowchartDirection
        from ___ in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        select (FlowStatement) new DirectionStatement(dir);

    // Any non-empty line that matched no earlier rule. Consuming it (rather than failing) keeps one
    // unsupported construct - e.g. a `linkStyle` line, or a node line using syntax we don't parse - from
    // aborting the whole diagram or silently dropping every statement after it.
    static Parser<char, Unit> unknownLine =
        from _ in CommonParsers.InlineWhitespace
        from content in Token(_ => _ != '\r' && _ != '\n').AtLeastOnceString()
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Skip empty lines, comments, and unsupported directives
    static Parser<char, Unit> skipLine =
        OneOf(
            Try(clickDirective),
            Try(CommonParsers.InlineWhitespace.Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline))),
            unknownLine
        );

    public static Parser<char, FlowchartModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in Try(String("flowchart")).Or(String("graph"))
        from __ in CommonParsers.InlineWhitespace
        from direction in flowchartDirection.Optional()
        from ___ in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        from statements in ParseStatements()
        select BuildModel(direction.GetValueOrDefault(Direction.TopToBottom), statements);

    static Parser<char, List<FlowStatement>> ParseStatements()
    {
        var nodeStatement =
            CommonParsers.InlineWhitespace
                .Then(StatementParser)
                .Before(CommonParsers.InlineWhitespace.Then(CommonParsers.LineEnd))
                .Select(_ => (FlowStatement?) new NodeChainStatement(_.Nodes, _.Edges));

        var item = OneOf(
            Try(subgraphStart.Select(_ => (FlowStatement?) _)),
            Try(subgraphEnd.Select(_ => (FlowStatement?) _)),
            Try(directionStatement.Select(_ => (FlowStatement?) _)),
            // classDef before class (the latter is a prefix of the former); both before nodeStatement so a
            // styling line is never mis-parsed as a node named "class"/"style".
            Try(classDefDirective.Select(_ => (FlowStatement?) _)),
            Try(classDirective.Select(_ => (FlowStatement?) _)),
            Try(styleDirective.Select(_ => (FlowStatement?) _)),
            Try(nodeStatement),
            skipLine.Select(_ => (FlowStatement?) null));

        return item.Many().Select(_ => _.OfType<FlowStatement>().ToList());
    }

    static FlowchartModel BuildModel(Direction direction, List<FlowStatement> statements)
    {
        var model = new FlowchartModel
        {
            Direction = direction
        };

        var nodeDict = new Dictionary<string, Node>();
        var subgraphStack = new Stack<Subgraph>();
        var assignedToSubgraph = new HashSet<string>();

        // Styling is declarative (forward references allowed), so directives are collected here and
        // resolved onto nodes/subgraphs in one pass after all statements are seen.
        var classDefs = new Dictionary<string, NodeStyle>(StringComparer.Ordinal);
        var inlineStyles = new Dictionary<string, NodeStyle>(StringComparer.Ordinal);
        var classAssignments = new List<(string Id, string ClassName)>();
        var subgraphById = new Dictionary<string, Subgraph>(StringComparer.Ordinal);

        foreach (var statement in statements)
        {
            switch (statement)
            {
                case SubgraphStartStatement start:
                    var subgraph = new Subgraph
                    {
                        Id = start.Id,
                        Title = start.Label ?? start.Id,
                        Direction = direction
                    };
                    if (subgraphStack.Count > 0)
                    {
                        subgraphStack.Peek().NestedSubgraphs.Add(subgraph);
                    }
                    else
                    {
                        model.Subgraphs.Add(subgraph);
                    }

                    subgraphById[subgraph.Id] = subgraph;
                    subgraphStack.Push(subgraph);
                    break;

                case SubgraphEndStatement:
                    if (subgraphStack.Count > 0)
                    {
                        subgraphStack.Pop();
                    }

                    break;

                case DirectionStatement dirStatement:
                    if (subgraphStack.Count > 0)
                    {
                        subgraphStack.Peek().Direction = dirStatement.Direction;
                    }
                    else
                    {
                        model.Direction = dirStatement.Direction;
                    }

                    break;

                case ClassDefStatement classDef:
                    var defStyle = ParseStyleProps(classDef.Props);
                    foreach (var name in SplitList(classDef.Names))
                    {
                        classDefs[name] = classDefs.TryGetValue(name, out var existingDef)
                            ? existingDef.MergedWith(defStyle)
                            : defStyle;
                    }

                    break;

                case ClassAssignStatement classAssign:
                    var className = classAssign.ClassName.TrimEnd(';');
                    foreach (var id in SplitList(classAssign.Ids))
                    {
                        classAssignments.Add((id, className));
                    }

                    break;

                case StyleStatement styleStatement:
                    var inline = ParseStyleProps(styleStatement.Props);
                    inlineStyles[styleStatement.Id] = inlineStyles.TryGetValue(styleStatement.Id, out var existingInline)
                        ? existingInline.MergedWith(inline)
                        : inline;
                    break;

                case NodeChainStatement chain:
                    for (var i = 0; i < chain.Nodes.Count; i++)
                    {
                        var node = chain.Nodes[i];

                        // Add or update node.
                        if (!nodeDict.TryGetValue(node.Id, out var existingNode))
                        {
                            nodeDict[node.Id] = node;
                            model.Nodes.Add(node);
                        }
                        else
                        {
                            if (node.Label != null &&
                                existingNode.Label == null)
                            {
                                existingNode.Label = node.Label;
                                existingNode.Shape = node.Shape;
                            }

                            // Carry `:::class` from any later reference onto the canonical node.
                            foreach (var styleClass in node.Classes)
                            {
                                if (!existingNode.Classes.Contains(styleClass))
                                {
                                    existingNode.Classes.Add(styleClass);
                                }
                            }
                        }

                        // A node belongs to the subgraph it first appears inside,
                        // even if it was first referenced outside one.
                        if (subgraphStack.Count > 0 &&
                            assignedToSubgraph.Add(node.Id))
                        {
                            subgraphStack.Peek().NodeIds.Add(node.Id);
                        }

                        // Add edge to next node
                        if (i < chain.Edges.Count)
                        {
                            var edge = chain.Edges[i];
                            model.Edges.Add(
                                new()
                                {
                                    SourceId = chain.Nodes[i].Id,
                                    TargetId = chain.Nodes[i + 1].Id,
                                    Type = edge.Type,
                                    LineStyle = edge.Style,
                                    Label = edge.Label
                                });
                        }
                    }

                    break;
            }
        }

        // Apply deferred class assignments to nodes (preferred) or subgraphs of the same id.
        foreach (var (id, className) in classAssignments)
        {
            if (nodeDict.TryGetValue(id, out var node))
            {
                if (!node.Classes.Contains(className))
                {
                    node.Classes.Add(className);
                }
            }
            else if (subgraphById.TryGetValue(id, out var subgraph) &&
                     !subgraph.Classes.Contains(className))
            {
                subgraph.Classes.Add(className);
            }
        }

        // `classDef default` styles every node; subgraphs only take explicitly assigned classes/styles.
        classDefs.TryGetValue("default", out var defaultStyle);

        NodeStyle? Resolve(List<string> classes, string id, NodeStyle? baseStyle)
        {
            var style = baseStyle;
            foreach (var className in classes)
            {
                if (classDefs.TryGetValue(className, out var classStyle))
                {
                    style = style is null ? classStyle : style.MergedWith(classStyle);
                }
            }

            if (inlineStyles.TryGetValue(id, out var inlineStyle))
            {
                style = style is null ? inlineStyle : style.MergedWith(inlineStyle);
            }

            return style;
        }

        foreach (var node in model.Nodes)
        {
            node.Style = Resolve(node.Classes, node.Id, defaultStyle);
        }

        foreach (var subgraph in subgraphById.Values)
        {
            subgraph.Style = Resolve(subgraph.Classes, subgraph.Id, null);
        }

        return model;
    }

    static IEnumerable<string> SplitList(string raw) =>
        raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    // Parses a Mermaid style property list, e.g. `fill:#f9f,stroke:#333,stroke-width:2px`.
    static NodeStyle ParseStyleProps(string raw)
    {
        var style = new NodeStyle();
        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = part.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            var key = part[..colon].Trim().ToLowerInvariant();
            // A trailing ';' terminates the directive in Mermaid; drop it from the final value.
            var value = part[(colon + 1)..].Trim().TrimEnd(';').Trim();
            if (value.Length == 0)
            {
                continue;
            }

            switch (key)
            {
                case "fill":
                    style.Fill = value;
                    break;
                case "stroke":
                    style.Stroke = value;
                    break;
                case "stroke-width":
                    if (TryParseWidth(value, out var width))
                    {
                        style.StrokeWidth = width;
                    }

                    break;
                case "color":
                    style.Color = value;
                    break;
                case "stroke-dasharray":
                    style.StrokeDasharray = value;
                    break;
            }
        }

        return style;
    }

    static bool TryParseWidth(string value, out double width)
    {
        var trimmed = value.EndsWith("px", StringComparison.OrdinalIgnoreCase)
            ? value[..^2].Trim()
            : value.Trim();
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out width);
    }

    public Result<char, FlowchartModel> Parse(string input) => Parser.Parse(input);

    abstract record FlowStatement;

    sealed record NodeChainStatement(
        List<Node> Nodes,
        List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges) : FlowStatement;

    sealed record SubgraphStartStatement(string Id, string? Label) : FlowStatement;

    sealed record SubgraphEndStatement : FlowStatement;

    sealed record DirectionStatement(Direction Direction) : FlowStatement;

    sealed record ClassDefStatement(string Names, string Props) : FlowStatement;

    sealed record ClassAssignStatement(string Ids, string ClassName) : FlowStatement;

    sealed record StyleStatement(string Id, string Props) : FlowStatement;
}

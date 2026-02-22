namespace MermaidSharp.Diagrams.Flowchart;

public class FlowchartParser : IDiagramParser<FlowchartModel>
{
    public DiagramType DiagramType => DiagramType.Flowchart;

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
            rectangleShape
        );

    // Node parser: identifier optionally followed by shape
    static Parser<char, Node> nodeParser =
        from id in CommonParsers.Identifier
        from shape in nodeShapeParser.Optional()
        select new Node
        {
            Id = id,
            Label = shape.HasValue ? shape.Value.Label : null,
            Shape = shape.HasValue ? shape.Value.Shape : NodeShape.Rectangle
        };

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
    static Parser<char, (List<Node> Nodes, List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges)> StatementParser =>
        from first in nodeParser
        from rest in (
            from _1 in CommonParsers.InlineWhitespace
            from label1 in edgeLabelParser.Optional()
            from _2 in CommonParsers.InlineWhitespace
            from arrow in arrowTypeParser
            from _3 in CommonParsers.InlineWhitespace
            from label2 in edgeLabelParser.Optional()
            from _4 in CommonParsers.InlineWhitespace
            from node in nodeParser
            select (node, arrow.Type, arrow.Style, label1.HasValue ? label1.Value : label2.HasValue ? label2.Value : null)
        ).Many()
        select (
            new List<Node>([first, .. rest.Select(_ => _.node)]),
            rest.Select(_ => (_.Type, _.Style, (string?)_.Item4)).ToList()
        );

    // Style directive: style NodeName fill:#color,stroke:#color
    static Parser<char, Unit> styleDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("style")
        from ___ in CommonParsers.RequiredWhitespace
        from ____ in Token(_ => _ != '\r' && _ != '\n').ManyString() // consume rest of line
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Class definition: classDef className fill:#color
    static Parser<char, Unit> classDefDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("classDef")
        from ___ in CommonParsers.RequiredWhitespace
        from ____ in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Class application: class nodeId className
    static Parser<char, Unit> classDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("class")
        from ___ in CommonParsers.RequiredWhitespace
        from ____ in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Click directive: click nodeId callback
    static Parser<char, Unit> clickDirective =
        from _ in CommonParsers.InlineWhitespace
        from __ in String("click")
        from ___ in CommonParsers.RequiredWhitespace
        from ____ in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Subgraph start: subgraph name[Label] or subgraph name
    static Parser<char, Unit> subgraphStart =
        from _ in CommonParsers.InlineWhitespace
        from subGraph in String("subgraph")
        from ___ in Token(_ => _ != '\r' && _ != '\n').ManyString()
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Subgraph end: end
    static Parser<char, Unit> subgraphEnd =
        from _ in CommonParsers.InlineWhitespace
        from end in String("end")
        from ___ in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        select Unit.Value;

    // Skip empty lines, comments, and directives
    static Parser<char, Unit> skipLine =
        OneOf(
            Try(styleDirective),
            Try(classDefDirective),
            Try(classDirective),
            Try(clickDirective),
            Try(subgraphStart),
            Try(subgraphEnd),
            CommonParsers.InlineWhitespace.Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline))
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

    static Parser<char, List<(List<Node> Nodes, List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges)>> ParseStatements()
    {
        var statement =
            CommonParsers.InlineWhitespace
                .Then(StatementParser)
                .Before(CommonParsers.InlineWhitespace.Then(CommonParsers.LineEnd));

        var skipLine = FlowchartParser.skipLine.ThenReturn((new List<Node>(), new List<(EdgeType, EdgeStyle, string?)>()));

        return Try(statement).Or(skipLine).Many()
            .Select(_ => _.Where(__ => __.Nodes.Count > 0).ToList());
    }

    static FlowchartModel BuildModel(Direction direction,
        List<(List<Node> Nodes, List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges)> statements)
    {
        var model = new FlowchartModel { Direction = direction };
        var nodeDict = new Dictionary<string, Node>();

        foreach (var (nodes, edges) in statements)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // Add or update node
                if (!nodeDict.TryGetValue(node.Id, out var existingNode))
                {
                    nodeDict[node.Id] = node;
                    model.Nodes.Add(node);
                }
                else if (node.Label != null && existingNode.Label == null)
                {
                    existingNode.Label = node.Label;
                    existingNode.Shape = node.Shape;
                }

                // Add edge to next node
                if (i < edges.Count)
                {
                    var edge = edges[i];
                    model.Edges.Add(new()
                    {
                        SourceId = nodes[i].Id,
                        TargetId = nodes[i + 1].Id,
                        Type = edge.Type,
                        LineStyle = edge.Style,
                        Label = edge.Label
                    });
                }
            }
        }

        return model;
    }

    public Result<char, FlowchartModel> Parse(string input) => Parser.Parse(input);
}

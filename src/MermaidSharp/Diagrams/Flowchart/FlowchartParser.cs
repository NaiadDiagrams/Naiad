using MermaidSharp;
using MermaidSharp.Models;
using MermaidSharp.Parsing;
using Pidgin;
using static Pidgin.Parser;
using static Pidgin.Parser<char>;

namespace MermaidSharp.Diagrams.Flowchart;

public class FlowchartParser : IDiagramParser<FlowchartModel>
{
    public DiagramType DiagramType => DiagramType.Flowchart;

    // Node shape parsers - returns (label, shape)
    static readonly Parser<char, (string Label, NodeShape Shape)> DoubleCircleShape =
        String("(((")
            .Then(Token(c => c != ')').ManyString())
            .Before(String(")))"))
            .Select(text => (text, NodeShape.DoubleCircle));

    static readonly Parser<char, (string Label, NodeShape Shape)> CircleShape =
        String("((")
            .Then(Token(c => c != ')').ManyString())
            .Before(String("))"))
            .Select(text => (text, NodeShape.Circle));

    static readonly Parser<char, (string Label, NodeShape Shape)> StadiumShape =
        String("([")
            .Then(Token(c => c != ']').ManyString())
            .Before(String("])"))
            .Select(text => (text, NodeShape.Stadium));

    static readonly Parser<char, (string Label, NodeShape Shape)> SubroutineShape =
        String("[[")
            .Then(Token(c => c != ']').ManyString())
            .Before(String("]]"))
            .Select(text => (text, NodeShape.Subroutine));

    static readonly Parser<char, (string Label, NodeShape Shape)> CylinderShape =
        String("[(")
            .Then(Token(c => c != ')').ManyString())
            .Before(String(")]"))
            .Select(text => (text, NodeShape.Cylinder));

    static readonly Parser<char, (string Label, NodeShape Shape)> HexagonShape =
        String("{{")
            .Then(Token(c => c != '}').ManyString())
            .Before(String("}}"))
            .Select(text => (text, NodeShape.Hexagon));

    static readonly Parser<char, (string Label, NodeShape Shape)> DiamondShape =
        Char('{')
            .Then(Token(c => c != '}').ManyString())
            .Before(Char('}'))
            .Select(text => (text, NodeShape.Diamond));

    static readonly Parser<char, (string Label, NodeShape Shape)> RoundedShape =
        Char('(')
            .Then(Token(c => c != ')').ManyString())
            .Before(Char(')'))
            .Select(text => (text, NodeShape.RoundedRectangle));

    static readonly Parser<char, (string Label, NodeShape Shape)> RectangleShape =
        Char('[')
            .Then(Token(c => c != ']').ManyString())
            .Before(Char(']'))
            .Select(text => (text, NodeShape.Rectangle));

    static readonly Parser<char, (string Label, NodeShape Shape)> AsymmetricShape =
        Char('>')
            .Then(Token(c => c != ']').ManyString())
            .Before(Char(']'))
            .Select(text => (text, NodeShape.Asymmetric));

    static readonly Parser<char, (string Label, NodeShape Shape)> NodeShapeParser =
        OneOf(
            Try(DoubleCircleShape),
            Try(CircleShape),
            Try(StadiumShape),
            Try(SubroutineShape),
            Try(CylinderShape),
            Try(HexagonShape),
            Try(DiamondShape),
            Try(RoundedShape),
            Try(AsymmetricShape),
            RectangleShape
        );

    // Node parser: identifier optionally followed by shape
    static readonly Parser<char, Node> NodeParser =
        from id in CommonParsers.Identifier
        from shape in NodeShapeParser.Optional()
        select new Node
        {
            Id = id,
            Label = shape.HasValue ? shape.Value.Label : null,
            Shape = shape.HasValue ? shape.Value.Shape : NodeShape.Rectangle
        };

    // Arrow parsers
    static readonly Parser<char, (EdgeType Type, EdgeStyle Style)> ArrowTypeParser =
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
    static readonly Parser<char, string> EdgeLabelParser =
        Char('|')
            .Then(Token(c => c != '|').ManyString())
            .Before(Char('|'));

    // Direction parser
    static readonly Parser<char, Direction> FlowchartDirection =
        OneOf(
            Try(String("TB")).ThenReturn(Direction.TopToBottom),
            Try(String("TD")).ThenReturn(Direction.TopToBottom),
            Try(String("BT")).ThenReturn(Direction.BottomToTop),
            Try(String("LR")).ThenReturn(Direction.LeftToRight),
            String("RL").ThenReturn(Direction.RightToLeft)
        );

    // Statement: A --> B --> C (chain of nodes with edges)
    static Parser<char, (List<Node> Nodes, List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges)> StatementParser =>
        from first in NodeParser
        from rest in (
            from _1 in CommonParsers.InlineWhitespace
            from label1 in EdgeLabelParser.Optional()
            from _2 in CommonParsers.InlineWhitespace
            from arrow in ArrowTypeParser
            from _3 in CommonParsers.InlineWhitespace
            from label2 in EdgeLabelParser.Optional()
            from _4 in CommonParsers.InlineWhitespace
            from node in NodeParser
            select (node, arrow.Type, arrow.Style, label1.HasValue ? label1.Value : (label2.HasValue ? label2.Value : null))
        ).Many()
        select (
            new List<Node>([first, .. rest.Select(r => r.node)]),
            rest.Select(r => (r.Type, r.Style, (string?)r.Item4)).ToList()
        );

    // Skip empty lines and comments
    static readonly Parser<char, Unit> SkipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    public Parser<char, FlowchartModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in Try(String("flowchart")).Or(String("graph"))
        from __ in CommonParsers.InlineWhitespace
        from direction in FlowchartDirection.Optional()
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from statements in ParseStatements()
        select BuildModel(direction.GetValueOrDefault(Direction.TopToBottom), statements);

    static Parser<char, List<(List<Node> Nodes, List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges)>> ParseStatements()
    {
        var statement =
            CommonParsers.InlineWhitespace
                .Then(StatementParser)
                .Before(CommonParsers.InlineWhitespace.Then(CommonParsers.LineEnd));

        var skipLine = SkipLine.ThenReturn((new List<Node>(), new List<(EdgeType, EdgeStyle, string?)>()));

        return Try(statement).Or(skipLine).Many()
            .Select(s => s.Where(x => x.Item1.Count > 0).ToList());
    }

    static FlowchartModel BuildModel(Direction direction,
        List<(List<Node> Nodes, List<(EdgeType Type, EdgeStyle Style, string? Label)> Edges)> statements)
    {
        var model = new FlowchartModel { Direction = direction };
        var nodeDict = new Dictionary<string, Node>();

        foreach (var (nodes, edges) in statements)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                // Add or update node
                if (!nodeDict.TryGetValue(node.Id, out var existingNode))
                {
                    nodeDict[node.Id] = node;
                    model.Nodes.Add(node);
                    existingNode = node;
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
                    model.Edges.Add(new Edge
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

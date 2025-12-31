namespace MermaidSharp.Diagrams.Treemap;

public class TreemapParser : IDiagramParser<TreemapModel>
{
    public DiagramType DiagramType => DiagramType.Treemap;

    // Quoted string: "text"
    static readonly Parser<char, string> QuotedString =
        Char('"').Then(Token(c => c != '"').ManyString()).Before(Char('"'));

    // Number
    static readonly Parser<char, double> Number =
        from neg in Char('-').Optional()
        from digits in Digit.AtLeastOnceString()
        from dec in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse((neg.HasValue ? "-" : "") + digits + (dec.HasValue ? "." + dec.Value : ""));

    // CSS class: :::className
    static readonly Parser<char, string> CssClass =
        String(":::").Then(Token(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').AtLeastOnceString());

    // Node line
    record NodeLine(int Indent, string Name, double? Value, string? CssClass);

    static readonly Parser<char, NodeLine> NodeLineParser =
        from indent in CommonParsers.Indentation
        from name in QuotedString
        from value in (
            from _ in CommonParsers.InlineWhitespace
            from __ in Char(':')
            from ___ in CommonParsers.InlineWhitespace
            from v in Number
            select v
        ).Optional()
        from cssClass in (
            from _ in CommonParsers.InlineWhitespace
            from cls in CssClass
            select cls
        ).Optional()
        from _ in CommonParsers.InlineWhitespace
        from __ in CommonParsers.LineEnd
        select new NodeLine(indent, name, value.GetValueOrDefault(), cssClass.GetValueOrDefault());

    // Skip line
    static readonly Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, NodeLine?> ContentItem =>
        Try(NodeLineParser.Select(n => (NodeLine?)n))
            .Or(SkipLine.ThenReturn((NodeLine?)null));

    public static Parser<char, TreemapModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("treemap-beta")
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from lines in ContentItem.ManyThen(End)
        select BuildModel(lines.Item1.Where(l => l != null).ToList());

    static TreemapModel BuildModel(List<NodeLine> lines)
    {
        var model = new TreemapModel();
        var stack = new Stack<(TreemapNode node, int indent)>();

        foreach (var line in lines)
        {
            var node = new TreemapNode
            {
                Name = line.Name,
                Value = line.Value,
                CssClass = line.CssClass
            };

            // Pop nodes from stack until we find parent
            while (stack.Count > 0 && stack.Peek().indent >= line.Indent)
            {
                stack.Pop();
            }

            if (stack.Count == 0)
            {
                // Root level node
                model.RootNodes.Add(node);
            }
            else
            {
                // Child of current parent
                stack.Peek().node.Children.Add(node);
            }

            // Push this node as potential parent
            stack.Push((node, line.Indent));
        }

        return model;
    }

    public Result<char, TreemapModel> Parse(string input) => Parser.Parse(input);
}

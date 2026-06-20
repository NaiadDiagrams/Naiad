class TreemapParser : IDiagramParser<TreemapModel>
{
    // Quoted string: "text"
    static Parser<char, string> quotedString =
        Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

    // Number
    static Parser<char, double> number = CommonParsers.SignedDecimal;

    // CSS class: :::className
    static Parser<char, string> cssClass =
        String(":::").Then(Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString());

    // Node line
    internal record NodeLine(int Indent, string Name, double? Value, string? CssClass);

    public static Parser<char, NodeLine> nodeLineParser =
        from indent in CommonParsers.Indentation
        from name in quotedString
        from value in (
            from _ in CommonParsers.InlineWhitespace
            from __ in Char(':')
            from ___ in CommonParsers.InlineWhitespace
            from v in number
            select v
        ).Optional()
        from cssClass in (
            from _ in CommonParsers.InlineWhitespace
            from cls in cssClass
            select cls
        ).Optional()
        from _ in CommonParsers.InlineWhitespace
        from __ in CommonParsers.LineEnd
        select new NodeLine(indent, name, value.GetValueOrDefault(), cssClass.GetValueOrDefault());

    // Skip line
    public static Parser<char, Unit> skipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    public static Parser<char, NodeLine?> ContentItem =>
        Try(nodeLineParser.Select<NodeLine?>(_ => _))
            .Or(skipLine.ThenReturn<NodeLine?>(null));

    public static Parser<char, TreemapModel> Parser =>
        from whitespance in CommonParsers.InlineWhitespace
        from ciString in CIString("treemap-beta")
        from innerWhitespace in CommonParsers.InlineWhitespace
        from lineEnd in CommonParsers.LineEnd
        from lines in ContentItem.ManyThen(End)
        select BuildModel(lines.Item1.Where(_ => _ != null).ToList());

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
            while (stack.TryPeek(out var top) && top.indent >= line.Indent)
            {
                stack.Pop();
            }

            if (stack.TryPeek(out var parent))
            {
                // Child of current parent
                parent.node.Children.Add(node);
            }
            else
            {
                // Root level node
                model.RootNodes.Add(node);
            }

            // Push this node as potential parent
            stack.Push((node, line.Indent));
        }

        return model;
    }

    public Result<char, TreemapModel> Parse(string input) => Parser.Parse(input);
}

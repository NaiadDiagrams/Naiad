class MindmapParser : IDiagramParser<MindmapModel>
{
    static readonly Parser<char, MindmapModel> Parser;

    static MindmapParser()
    {
        // Parse indentation (spaces or tabs)
        var indentationParser =
            Token(_ => _ is ' ' or '\t')
                .Many()
                .Select(chars =>
                {
                    var array = chars as char[] ?? chars.ToArray();
                    return array.Count(_ => _ == '\t') * 4 + array.Count(_ => _ == ' ');
                });

        // Icon: ::icon(fa fa-book)
        var iconParser =
            from _ in String("::icon(")
            from icon in Token(_ => _ != ')').AtLeastOnceString()
            from __ in Char(')')
            select icon;

        // CSS class: :::className
        var cssClassParser =
            from _ in String(":::")
            from cls in Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString()
            select cls;

        // Node with shape: ((circle)), (rounded), [square], {{hexagon}}, ))bang((, )cloud(
        var shapedNodeParser =
            OneOf(
                // Circle: ((text))
                Try(
                    from _ in String("((")
                    from text in Token(_ => _ != ')').AtLeastOnceString()
                    from __ in String("))")
                    select (text, shape: MindmapShape.Circle)
                ),
                // Bang/explosion: ))text((
                Try(
                    from _ in String("))")
                    from text in Token(_ => _ != '(').AtLeastOnceString()
                    from __ in String("((")
                    select (text, shape: MindmapShape.Bang)
                ),
                // Cloud: )text(
                Try(
                    from _ in Char(')')
                    from text in Token(_ => _ != '(').AtLeastOnceString()
                    from __ in Char('(')
                    select (text, shape: MindmapShape.Cloud)
                ),
                // Hexagon: {{text}}
                Try(
                    from _ in String("{{")
                    from text in Token(_ => _ != '}').AtLeastOnceString()
                    from __ in String("}}")
                    select (text, shape: MindmapShape.Hexagon)
                ),
                // Rounded: (text)
                Try(
                    from _ in Char('(')
                    from text in Token(_ => _ != ')').AtLeastOnceString()
                    from __ in Char(')')
                    select (text, shape: MindmapShape.Rounded)
                ),
                // Square: [text]
                Try(
                    from _ in Char('[')
                    from text in Token(_ => _ != ']').AtLeastOnceString()
                    from __ in Char(']')
                    select (text, shape: MindmapShape.Square)
                )
            );

        // Optional node id preceding a shape, e.g. the "root" in root((text)). Mindmap
        // nodes have no edges, so the id is not used when rendering — the shape's text
        // is the label.
        var nodeIdParser =
            Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

        // A shape with an optional leading id: id((circle)), id[square], or just ((circle)).
        var shapedNodeWithIdParser =
            from _ in Try(nodeIdParser).Optional()
            from shaped in shapedNodeParser
            select shaped;

        // Node line: indentation + optional shape + text + optional icon/class
        var nodeLineParser =
            from indent in indentationParser
            from shaped in Try(shapedNodeWithIdParser).Optional()
            from plainText in shaped.HasValue
                ? Return("")
                : Token(_ => _ != ':' && _ != '\r' && _ != '\n').ManyString()
            from _ in CommonParsers.InlineWhitespace
            from icon in Try(iconParser).Optional()
            from __ in CommonParsers.InlineWhitespace
            from cssClass in Try(cssClassParser).Optional()
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            select (
                indent,
                shaped.HasValue ? shaped.Value.text.Trim() : plainText.Trim(),
                shaped.HasValue ? shaped.Value.shape : MindmapShape.Default,
                icon.HasValue ? icon.Value : null,
                cssClass.HasValue ? cssClass.Value : null
            );

        // Content line - node line, skip line (comment/empty), or end
        var contentLine =
            OneOf(
                Try(nodeLineParser.Select(_ => ((int, string, MindmapShape, string?, string?)?)_)),
                Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                    .ThenReturn(((int, string, MindmapShape, string?, string?)?)null),
                Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline))
                    .ThenReturn(((int, string, MindmapShape, string?, string?)?)null)
            );

        Parser =
            from _ in CommonParsers.InlineWhitespace
            from __ in CIString("mindmap")
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            from result in contentLine.ManyThen(End)
            select BuildModel(result.Item1.Where(_ => _.HasValue).Select(_ => _!.Value).ToList());
    }

    static MindmapModel BuildModel(List<(int indent, string text, MindmapShape shape, string? icon, string? cssClass)> lines)
    {
        var model = new MindmapModel();

        if (lines.Count == 0)
            return model;

        // Build tree from indentation
        var nodes = lines.Select((line, index) => new MindmapNode
        {
            Text = line.text,
            Shape = line.shape,
            Icon = line.icon,
            CssClass = line.cssClass,
            Level = index == 0 ? 0 : -1 // Root is level 0, others TBD
        }).ToList();

        // First node is root
        model.Root = nodes[0];
        model.Root.Level = 0;

        if (nodes.Count == 1)
            return model;

        // Calculate base indentation (from first node after root)
        var baseIndent = lines[0].indent;
        var indentStack = new Stack<(int indent, MindmapNode node)>();
        indentStack.Push((baseIndent, model.Root));

        for (var i = 1; i < lines.Count; i++)
        {
            var (indent, _, _, _, _) = lines[i];
            var node = nodes[i];

            // Pop stack until we find a parent with smaller indentation
            while (indentStack.TryPeek(out var top) &&
                   top.indent >= indent)
            {
                indentStack.Pop();
            }

            if (indentStack.Count == 0)
            {
                // This shouldn't happen with valid input, but treat as child of root
                indentStack.Push((baseIndent, model.Root));
            }

            var parent = indentStack.Peek().node;
            node.Level = parent.Level + 1;
            parent.Children.Add(node);

            indentStack.Push((indent, node));
        }

        return model;
    }

    public Result<char, MindmapModel> Parse(string input) => Parser.Parse(input);
}

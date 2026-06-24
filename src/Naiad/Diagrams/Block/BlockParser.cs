class BlockParser : IDiagramParser<BlockModel>
{
    static readonly Parser<char, string> identifier;

    // Label content (text inside shape brackets)
    static readonly Parser<char, string> labelContent;

    static readonly Parser<char, string> quotedLabel;

    static readonly Parser<char, int> columnsParser;

    // Rectangle shape: ["label"] or [label]
    static readonly Parser<char, (string label, BlockShape shape)> rectangleShape;

    // Rounded shape: ("label") or (label)
    static readonly Parser<char, (string label, BlockShape shape)> roundedShape;

    // Stadium shape: (["label"]) or ([label])
    static readonly Parser<char, (string label, BlockShape shape)> stadiumShape;

    // Circle shape: (("label")) or ((label))
    static readonly Parser<char, (string label, BlockShape shape)> circleShape;

    // Diamond shape: {"label"} or {label}
    static readonly Parser<char, (string label, BlockShape shape)> diamondShape;

    // Hexagon shape: {{"label"}} or {{label}}
    static readonly Parser<char, (string label, BlockShape shape)> hexagonShape;

    // Shape parser (order matters - more specific first)
    static readonly Parser<char, (string label, BlockShape shape)> shapeParser;

    // Span: :N
    static readonly Parser<char, int> spanParser;

    // Block element: id["label"]:2
    static readonly Parser<char, BlockElement> elementParser;

    // Elements on a line (space separated)
    static readonly Parser<char, List<BlockElement>> elementsLineParser;

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine;

    // Content item
    static readonly Parser<char, IBlockContent?> ContentItem;

    static readonly Parser<char, BlockModel> Parser;

    static BlockParser()
    {
        identifier =
            Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-').AtLeastOnceString();

        labelContent =
            Token(_ => _ != '"' && _ != ']' && _ != ')' && _ != '}').ManyString();

        quotedLabel =
            Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

        columnsParser =
            from inlineWhitespace in CommonParsers.InlineWhitespace
            from columns in CIString("columns")
            from rRequiredWhitespace in CommonParsers.RequiredWhitespace
            from num in Digit.AtLeastOnceString().Select(int.Parse)
            from innerInlineWhitespace in CommonParsers.InlineWhitespace
            from lineEnd in CommonParsers.LineEnd
            select num;

        rectangleShape =
            from left in Char('[')
            from label in quotedLabel.Or(labelContent)
            from right in Char(']')
            select (label.Trim(), BlockShape.Rectangle);

        roundedShape =
            from left in Char('(')
            from label in quotedLabel.Or(Token(_ => _ != ')').ManyString())
            from right in Char(')')
            select (label.Trim(), BlockShape.Rounded);

        stadiumShape =
            from left in String("([")
            from label in quotedLabel.Or(Token(_ => _ != ']').ManyString())
            from right in String("])")
            select (label.Trim(), BlockShape.Stadium);

        circleShape =
            from left in String("((")
            from label in quotedLabel.Or(Token(_ => _ != ')').ManyString())
            from right in String("))")
            select (label.Trim(), BlockShape.Circle);

        diamondShape =
            from left in Char('{')
            from notDouble in Lookahead(AnyCharExcept('{'))
            from label in quotedLabel.Or(Token(_ => _ != '}').ManyString())
            from right in Char('}')
            select (label.Trim(), BlockShape.Diamond);

        hexagonShape =
            from left in String("{{")
            from label in quotedLabel.Or(Token(_ => _ != '}').ManyString())
            from right in String("}}")
            select (label.Trim(), BlockShape.Hexagon);

        shapeParser =
            OneOf(
                Try(stadiumShape),
                Try(circleShape),
                Try(hexagonShape),
                Try(diamondShape),
                Try(roundedShape),
                Try(rectangleShape)
            );

        spanParser =
            from _ in Char(':')
            from num in Digit.AtLeastOnceString().Select(int.Parse)
            select num;

        elementParser =
            from id in identifier
            from shape in shapeParser.Optional()
            from span in spanParser.Optional()
            select new BlockElement
            {
                Id = id,
                Label = shape.HasValue ? shape.Value.label : id,
                Shape = shape.HasValue ? shape.Value.shape : BlockShape.Rectangle,
                Span = span.GetValueOrDefault(1)
            };

        elementsLineParser =
            from _ in CommonParsers.InlineWhitespace
            from elements in elementParser.SeparatedAtLeastOnce(CommonParsers.RequiredWhitespace)
            from __ in CommonParsers.InlineWhitespace
            from ___ in CommonParsers.LineEnd
            select elements.ToList();

        skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        ContentItem =
            OneOf(
                Try(columnsParser.Select<IBlockContent?>(_ => new ColumnsItem(_))),
                Try(elementsLineParser.Select<IBlockContent?>(_ => new ElementsItem(_))),
                skipLine.ThenReturn<IBlockContent?>(null)
            );

        Parser =
            from _ in CommonParsers.InlineWhitespace
            from __ in OneOf(CIString("block-beta"), CIString("block"))
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            from result in ContentItem.ManyThen(End)
            select BuildModel(result.Item1);
    }

    static BlockModel BuildModel(IEnumerable<IBlockContent?> content)
    {
        var model = new BlockModel();

        foreach (var item in content)
        {
            switch (item)
            {
                case ColumnsItem columns:
                    model.Columns = columns.Count;
                    break;

                case ElementsItem elements:
                    model.Elements.AddRange(elements.Elements);
                    break;
            }
        }

        return model;
    }

    public Result<char, BlockModel> Parse(string input) => Parser.Parse(input);

    internal interface IBlockContent;
    readonly record struct ColumnsItem(int Count) : IBlockContent;
    readonly record struct ElementsItem(List<BlockElement> Elements) : IBlockContent;
}

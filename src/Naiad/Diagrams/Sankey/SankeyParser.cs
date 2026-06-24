class SankeyParser : IDiagramParser<SankeyModel>
{
    static readonly Parser<char, double> numberParser;

    static readonly Parser<char, string> quotedString;

    // Unquoted name (no commas or newlines)
    static readonly Parser<char, string> unquotedName;

    // Name (quoted or unquoted)
    static readonly Parser<char, string> name;

    // Link: source,target,value
    static readonly Parser<char, SankeyLink> linkParser;

    // Skip line (comments, empty lines)
    static readonly Parser<char, Unit> skipLine;

    static readonly Parser<char, SankeyLink?> ContentItem;

    static readonly Parser<char, SankeyModel> Parser;

    static SankeyParser()
    {
        numberParser = CommonParsers.SignedDecimal;

        quotedString =
            Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

        unquotedName =
            Token(_ => _ != ',' && _ != '\r' && _ != '\n').AtLeastOnceString()
                .Select(_ => _.Trim());

        name =
            quotedString.Or(unquotedName);

        linkParser =
            from _ in CommonParsers.InlineWhitespace
            from source in name
            from __ in Char(',')
            from ___ in CommonParsers.InlineWhitespace
            from target in name
            from ____ in Char(',')
            from _____ in CommonParsers.InlineWhitespace
            from value in numberParser
            from ______ in CommonParsers.InlineWhitespace
            from _______ in CommonParsers.LineEnd
            select new SankeyLink
            {
                Source = source,
                Target = target,
                Value = value
            };

        skipLine =
            Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
                .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

        ContentItem =
            OneOf(
                Try(linkParser.Select<SankeyLink?>(_ => _)),
                skipLine.ThenReturn<SankeyLink?>(null)
            );

        Parser =
            from _ in CommonParsers.InlineWhitespace
            from __ in OneOf(CIString("sankey-beta"), CIString("sankey"))
            from ___ in CommonParsers.InlineWhitespace
            from ____ in CommonParsers.LineEnd
            from result in ContentItem.ManyThen(End)
            select BuildModel(result.Item1.Where(_ => _ != null).ToList());
    }

    static SankeyModel BuildModel(List<SankeyLink> links)
    {
        var model = new SankeyModel();
        model.Links.AddRange(links);
        return model;
    }

    public Result<char, SankeyModel> Parse(string input) => Parser.Parse(input);
}

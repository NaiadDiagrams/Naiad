namespace MermaidSharp.Diagrams.Sankey;

public class SankeyParser : IDiagramParser<SankeyModel>
{
    public DiagramType DiagramType => DiagramType.Sankey;

    // Number parser
    static Parser<char, double> NumberParser =
        from sign in Char('-').Optional()
        from integer in Digit.AtLeastOnceString()
        from frac in Char('.').Then(Digit.AtLeastOnceString()).Optional()
        select double.Parse(
            (sign.HasValue ? "-" : "") + integer + (frac.HasValue ? "." + frac.Value : ""),
            CultureInfo.InvariantCulture);

    // Quoted string
    static Parser<char, string> QuotedString =
        Char('"').Then(Token(_ => _ != '"').ManyString()).Before(Char('"'));

    // Unquoted name (no commas or newlines)
    static Parser<char, string> UnquotedName =
        Token(_ => _ != ',' && _ != '\r' && _ != '\n').AtLeastOnceString()
            .Select(_ => _.Trim());

    // Name (quoted or unquoted)
    static Parser<char, string> Name =
        QuotedString.Or(UnquotedName);

    // Link: source,target,value
    static Parser<char, SankeyLink> LinkParser =
        from _ in CommonParsers.InlineWhitespace
        from source in Name
        from __ in Char(',')
        from ___ in CommonParsers.InlineWhitespace
        from target in Name
        from ____ in Char(',')
        from _____ in CommonParsers.InlineWhitespace
        from value in NumberParser
        from ______ in CommonParsers.InlineWhitespace
        from _______ in CommonParsers.LineEnd
        select new SankeyLink
        {
            Source = source,
            Target = target,
            Value = value
        };

    // Skip line (comments, empty lines)
    static Parser<char, Unit> SkipLine =
        Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Comment))
            .Or(Try(CommonParsers.InlineWhitespace.Then(CommonParsers.Newline)));

    // Content item
    static Parser<char, SankeyLink?> ContentItem =>
        OneOf(
            Try(LinkParser.Select(_ => (SankeyLink?)_)),
            SkipLine.ThenReturn((SankeyLink?)null)
        );

    public static Parser<char, SankeyModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from __ in OneOf(CIString("sankey-beta"), CIString("sankey"))
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        from result in ContentItem.ManyThen(End)
        select BuildModel(result.Item1.Where(_ => _ != null).ToList());

    static SankeyModel BuildModel(List<SankeyLink> links)
    {
        var model = new SankeyModel();
        model.Links.AddRange(links);
        return model;
    }

    public Result<char, SankeyModel> Parse(string input) => Parser.Parse(input);
}

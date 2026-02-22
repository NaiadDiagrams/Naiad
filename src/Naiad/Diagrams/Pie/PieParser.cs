namespace MermaidSharp.Diagrams.Pie;

public class PieParser : IDiagramParser<PieModel>
{
    public DiagramType DiagramType => DiagramType.Pie;

    static Parser<char, PieSection> SectionParser =
        from _ in CommonParsers.InlineWhitespace
        from label in CommonParsers.QuotedString
        from __ in CommonParsers.InlineWhitespace
        from colon in Char(':')
        from ___ in CommonParsers.InlineWhitespace
        from value in CommonParsers.Number
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new PieSection { Label = label, Value = value };

    static Parser<char, string> TitleLine =
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("title")
        from __ in CommonParsers.RequiredWhitespace
        from title in Token(c => c != '\r' && c != '\n').ManyString()
        from ___ in CommonParsers.LineEnd
        select title;

    static Parser<char, bool> ShowDataParser =
        Try(String("showData")).ThenReturn(true).Or(Return(false));

    static Parser<char, Unit> SkipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Inline title: pie title My Title (on same line)
    static Parser<char, string> InlineTitleParser =
        from keyword in String("title")
        from _ in CommonParsers.RequiredWhitespace
        from title in Token(c => c != '\r' && c != '\n').ManyString()
        select title;

    public static Parser<char, PieModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in String("pie")
        from __ in CommonParsers.InlineWhitespace
        from showData in ShowDataParser
        from ___ in CommonParsers.InlineWhitespace
        from inlineTitle in Try(InlineTitleParser).Optional()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        from content in ParseContent()
        select BuildModel(showData, inlineTitle.HasValue ? inlineTitle.Value : content.title, content.sections);

    static Parser<char, (string? title, List<PieSection> sections)> ParseContent() =>
        from lines in Try(TitleLine.Select(_ => (title: (string?)_, section: (PieSection?)null)))
            .Or(Try(SectionParser.Select(_ => (title: (string?)null, section: (PieSection?)_))))
            .Or(SkipLine.ThenReturn((title: (string?)null, section: (PieSection?)null))).Many()
        select (
            title: lines.FirstOrDefault(l => l.title != null).title,
            sections: lines.Where(l => l.section != null).Select(l => l.section!).ToList()
        );

    static PieModel BuildModel(bool showData, string? title, List<PieSection> sections)
    {
        var model = new PieModel
        {
            ShowData = showData,
            Title = title
        };
        model.Sections.AddRange(sections);
        return model;
    }

    public Result<char, PieModel> Parse(string input) => Parser.Parse(input);
}
